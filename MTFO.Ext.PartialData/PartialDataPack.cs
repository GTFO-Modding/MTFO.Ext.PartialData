using MTFO.Ext.PartialData.DataBlockTypes;
using MTFO.Ext.PartialData.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MTFO.Ext.PartialData
{
    public class PartialDataPack
    {
        public string Namespace { get; private set; } = string.Empty;
        public bool CheckFileChange { get; set; } = true;

        private readonly List<string> _AddedFiles = new();
        private readonly List<PartialDataCache> _DataCaches = new();

        public PartialDataPack()
        {

        }

        public PartialDataPack(string namespaceString) : this()
        {
            Namespace = namespaceString;
        }

        public string GetGUIDFormat(string guid)
        {
            if (!string.IsNullOrEmpty(Namespace))
            {
                guid = $"{Namespace}.{guid}";
            }

            return guid;
        }

        public void ClearPack()
        {
            _AddedFiles.Clear();
            _DataCaches.Clear();
        }

        public void ReadPack(string packPath)
        {
            var files = Directory.GetFiles(packPath, "*.json", SearchOption.AllDirectories).OrderBy(f => f);
            foreach (var file in files)
            {
                if (Path.GetFileName(file).StartsWith("_"))
                {
                    Logger.Log($"{file} have discard prefix (_) excluding from loader!");
                    continue;
                }

                if (!File.Exists(file))
                {
                    Logger.Error($"File ({file}) is not exist somehow?");
                    continue;
                }

                if (_AddedFiles.Contains(file))
                {
                    Logger.Error($"File ({file}) has loaded multiple times!");
                    continue;
                }

                _AddedFiles.Add(file);

                AllocateGUIDFromFile(file);

                Logger.Log($" - {file}");
            }

            if (CheckFileChange)
            {
                //TODO: HI
            }
        }

        public void AddToGame()
        {
            foreach (var cache in _DataCaches)
            {
                while (cache.JsonsToRead.Count > 0)
                {
                    var json = cache.JsonsToRead.Dequeue();
                    cache.DataBlockType.AddJsonBlock(json);
                }
            }
        }

        public void WriteGameDataFile(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var cache in _DataCaches)
            {
                var file = Path.Combine(path, "GameData_" + cache.DataBlockType.GetFullName() + "_bin.json");
                var fileFullPath = Path.GetFullPath(file);
                cache.DataBlockType.DoSaveToDisk(fileFullPath);
            }
        }

        private void AllocateGUIDFromFile(string file)
        {
            using var fs = File.OpenRead(file);
            using var doc = JsonDocument.Parse(fs, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            var root = doc.RootElement;
            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var element in root.EnumerateArray())
                    {
                        Read(element, true, file);
                    }
                    break;

                case JsonValueKind.Object:
                    Read(root, true, file);
                    break;
            }
        }

        private void OnDatablockChanged()
        {
            foreach (var cache in _DataCaches)
            {
                bool isChanged = false;
                while (cache.JsonsToRead.Count > 0)
                {
                    if (cache.Name.Equals("Rundown"))
                    {
                        cache.JsonsToRead.Clear(); //TODO: Someday, Fix it
                        Logger.Error("Editing Rundown DataBlock will leads to crash, Ignored");
                        continue;
                    }

                    var json = cache.JsonsToRead.Dequeue();
                    cache.DataBlockType.AddJsonBlock(json);
                    isChanged = true;
                }

                if (isChanged)
                {
                    cache.DataBlockType.OnChanged();
                }
            }
        }



        private void Read(JsonElement objNode, bool assignID, string debugName)
        {
            if (!objNode.TryGetProperty("persistentID", out var idNode))
            {
                Logger.Error($"persistentID field is missing: {debugName}");
                return;
            }

            if (!objNode.TryGetProperty("datablock", out var dbNode))
            {
                Logger.Error($"datablock field is missing: {debugName}");
                return;
            }

            if (assignID)
            {
                if (idNode.ValueKind == JsonValueKind.String)
                {
                    if (!DataBlockTypeManager.TryGetNextID(dbNode.GetString(), out var id))
                    {
                        Logger.Error($"datablock field is not valid: {debugName} {objNode}");
                        return;
                    }
                    PersistentIDManager.TryAssignId(idNode.GetString(), id);
                }
            }

            var datablockName = DataBlockTypeManager.GetBlockName(dbNode.GetString());
            if (!DataBlockTypeManager.TryFindCache(datablockName, out var cache))
            {
                Logger.Error($"datablock field is not valid: {debugName} {objNode}");
                return;
            }

            PartialDataCache partialDataCache = _DataCaches.FirstOrDefault(x => x.Name.Equals(datablockName));
            if (partialDataCache == null)
            {
                partialDataCache = new PartialDataCache(cache);
                _DataCaches.Add(partialDataCache);
            }

            var queueJson = objNode.ToString();
            switch (partialDataCache.DataBlockType.GetShortName())
            {
                case "PlayerOfflineGear":
                    if (!objNode.TryGetProperty("GearJSON", out var gearjsonNode))
                    {
                        Logger.Warning($"GearJSON field is missing, Ignore Stuff: {debugName}");
                        break;
                    }

                    if (gearjsonNode.ValueKind == JsonValueKind.String)
                    {
                        var gearjson = gearjsonNode.GetString();
                        if (GearJSONUtil.TryProcessGUID(gearjson, Namespace, out var outjson))
                        {
                            gearjson = gearjson.Replace("\"", "\\\"");
                            outjson = outjson.Replace("\"", "\\\"");
                            queueJson = queueJson.Replace(gearjson, outjson);
                            Logger.Warning(queueJson);
                        }
                    }
                    break;
            }

            partialDataCache.JsonsToRead.Enqueue(queueJson);
        }
    }
}
