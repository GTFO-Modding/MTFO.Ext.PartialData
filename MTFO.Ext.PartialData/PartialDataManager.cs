using GTFO.API.Utilities;
using MTFO.Ext.PartialData.DataBlockTypes;
using MTFO.Ext.PartialData.DTO;
using MTFO.Ext.PartialData.Interops;
using MTFO.Ext.PartialData.JsonConverters;
using MTFO.Ext.PartialData.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Logger = MTFO.Ext.PartialData.Utils.Logger;

namespace MTFO.Ext.PartialData
{
    public class PartialDataManager
    {
        public static string PartialDataPath { get; private set; }
        public static string ConfigPath { get; private set; }
        public static bool Initialized { get; private set; } = false;
        public static bool CanLiveEdit { get; set; } = false;
        public static PersistentIDConverter IDConverter { get; private set; } = new PersistentIDConverter();

        private static List<DataBlockDefinition> _Config;
        private static readonly List<string> _AddedFileList = new();
        private static readonly List<PartialDataCache> _DataCache = new();
        private static readonly Queue<JsonDocument> _DocumentsToDispose = new();

        internal static bool Initialize()
        {
            if (Initialized)
                return false;

            if (!MTFOInterop.IsLoaded)
                return false;

            PartialDataPath = Path.GetFullPath(Path.Combine(MTFOInterop.GameDataPath, "PartialData"));
            if (!Directory.Exists(PartialDataPath))
            {
                Logger.Error("Unable to setup PartialData::PartialData folder is missing");
                return false;
            }


            ConfigPath = Path.GetFullPath(Path.Combine(PartialDataPath, "_config.json"));
            if (!File.Exists(ConfigPath))
            {
                Logger.Error("Config File is missing! (_config.json) Creating new one!");
                File.WriteAllText(ConfigPath, "[]");
                return false;
            }


            _AddedFileList.Clear();
            _DataCache.Clear();
            _Config = JSON.Deserialize<List<DataBlockDefinition>>(File.ReadAllText(ConfigPath));

            Initialized = true;
            ReadAndAssignIDs();
            return true;
        }

        private static void ReadAndAssignIDs()
        {
            foreach (var def in _Config)
            {
                DataBlockTypeManager.SetIDBuffer(def.TypeName, def.StartFromID, def.IncrementMode);
            }

            var files = Directory.GetFiles(PartialDataPath, "*.json", SearchOption.AllDirectories).OrderBy(f => f);
            foreach (var file in files)
            {
                if (Path.GetFileName(file).StartsWith("_"))
                {
                    Logger.Debug($"{file} have discard prefix (_) excluding from loader!");
                    continue;
                }

                if (!File.Exists(file))
                {
                    Logger.Error($"File ({file}) is not exist somehow?");
                    continue;
                }

                if (_AddedFileList.Contains(file))
                {
                    Logger.Error($"File ({file}) has loaded multiple times!");
                    continue;
                }

                _AddedFileList.Add(file);

                AssignPersistentID(file);

                Logger.Debug($" - {file}");
            }
        }

        private static void AssignPersistentID(string file)
        {
            using var fs = File.OpenRead(file);

            var doc = JsonDocument.Parse(fs, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            _DocumentsToDispose.Enqueue(doc);

            var root = doc.RootElement;
            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var element in root.EnumerateArray())
                    {
                        Read(element, assignID: true, file);
                    }
                    break;

                case JsonValueKind.Object:
                    Read(root, assignID: true, file);
                    break;
            }
        }

        private static void ReadChangedFile(string path, string debugName)
        {
            using var fs = File.OpenRead(path);
            var doc = JsonDocument.Parse(fs, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            _DocumentsToDispose.Enqueue(doc);

            var root = doc.RootElement;
            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var element in root.EnumerateArray())
                    {
                        Read(element, assignID: false, debugName);
                    }
                    break;

                case JsonValueKind.Object:
                    Read(root, assignID: false, debugName);
                    break;
            }
        }

        private static void Read(JsonElement objNode, bool assignID, string debugName)
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

            PartialDataCache partialDataCache = _DataCache.FirstOrDefault(x => x.Name.Equals(datablockName));
            if (partialDataCache == null)
            {
                partialDataCache = new PartialDataCache(cache);
                _DataCache.Add(partialDataCache);
            }

            partialDataCache.JsonsToRead.Enqueue(objNode);
            cache.CacheInheritance(objNode, idNode);
        }

        internal static void LoadPartialData()
        {
            if (!Initialized)
                return;

            if (CanLiveEdit)
            {
                var sfw = SafeFileSystemWatcher.Create(PartialDataPath, ["*.json", "*.jsonc"], includeSubDir: true);
                sfw.OnChanged += OnFileChanged;
            }

            AddAllCache();
        }

        private static void OnFileChanged(FileEventArgs e)
        {
            Logger.Warning($"LiveEdit File Changed: {e.FullPath}");
            ReadChangedFile(e.FullPath, debugName: e.FullPath);
            AddAllCache(isLiveEdit: true);
        }

        internal static void WriteAllFile(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var cache in _DataCache)
            {
                var file = Path.Combine(path, "GameData_" + cache.DataBlockType.GetFullName() + "_bin.json");
                var fileFullPath = Path.GetFullPath(file);
                cache.DataBlockType.DoSaveToDisk(fileFullPath);
            }
        }

        private static void AddAllCache(bool isLiveEdit = false)
        {
            foreach (var cache in _DataCache)
            {
                bool isChanged = false;
                while (cache.JsonsToRead.TryDequeue(out var json))
                {
                    cache.DataBlockType.AddJsonBlock(json);
                    isChanged = true;
                }

                cache.DataBlockType.ApplyInheritance();

                if (isChanged && isLiveEdit)
                {
                    cache.DataBlockType.OnChanged();
                }
            }

            while (_DocumentsToDispose.TryDequeue(out var doc))
            {
                if (doc == null)
                    continue;

                doc.Dispose();
            }
        }

        public static uint GetID(string guid)
        {
            if (!Initialized)
                return 0;

            return PersistentIDManager.GetId(guid);
        }
    }
}