using MTFO.Ext.PartialData.Utils;
using System.Collections.Generic;
using System.IO;

namespace MTFO.Ext.PartialData
{
    public static class PersistentIDManager
    {
        private readonly static Dictionary<string, uint> _GUIDDict = new Dictionary<string, uint>();

        public static bool TryAssignId(string guid, uint id)
        {
            if (_GUIDDict.ContainsKey(guid))
            {
                Logger.Error($"GUID is already used: {guid}");
                return false;
            }

            _GUIDDict.Add(guid, id);
            return true;
        }

        public static uint GetId(string guid)
        {
            if (!_GUIDDict.TryGetValue(guid, out var id))
            {
                Logger.Error($"GUID is Missing: {guid}");
                return 0;
            }
            return id;
        }

        public static bool TryGetId(string guid, out uint id)
        {
            if (_GUIDDict.TryGetValue(guid, out id))
            {
                return true;
            }

            id = 0u;
            return false;
        }

        public static void DumpToFile(string path)
        {
            var text = "[\n\t//AUTO-GENERATED PERSISTENT ID LIST\n";
            foreach (var pair in _GUIDDict)
            {
                text += "\t{ \"GUID\": \"" + pair.Key + "\", \"ID\": " + pair.Value + " },\n";
            }

            if (text.Length > 2)
            {
                text = text[0..^2];
            }
            text += "\n]";
            File.WriteAllText(path, text);
        }
    }
}