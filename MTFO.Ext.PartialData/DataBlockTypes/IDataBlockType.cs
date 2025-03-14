using System;
using System.Text.Json;

namespace MTFO.Ext.PartialData.DataBlockTypes
{
    internal interface IDataBlockType
    {
        string GetShortName();

        string GetFullName();

        void DoSaveToDisk(string fullPath);

        void AddJsonBlock(string json);

        void OnChanged();

        void RegisterOnChangeEvent(Action onChanged);

        void CacheInheritance(JsonElement objNode, JsonElement idNode);

        void ApplyInheritance();
    }
}