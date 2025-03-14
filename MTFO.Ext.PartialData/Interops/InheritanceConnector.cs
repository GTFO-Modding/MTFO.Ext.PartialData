using GameData;
using InheritanceDataBlocks.API;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace MTFO.Ext.PartialData.Interops;
internal class InheritanceConnector<T> where T : GameDataBlockBase<T>
{
    private static bool HasInheritance => InheritanceInterop.IsLoaded;
    private readonly Queue<InheritanceCache> _caches;

    public InheritanceConnector()
    {
        if (HasInheritance)
            _caches = new();
    }

    private struct InheritanceCache
    {
        public uint id;
        public List<string> propertyNames;
        public uint parentID;
        public string parentStr;
    }

    public void CacheInheritance(JsonElement objNode, JsonElement idNode)
    {
        if (!HasInheritance) return;

        uint id = idNode.ValueKind == JsonValueKind.String ? PersistentIDManager.GetId(idNode.GetString()) : idNode.GetUInt32();
        RemoveInheritance_Internal(id);

        if (!objNode.TryGetProperty("parentID", out var parNode)) return;

        InheritanceCache cache = new()
        {
            propertyNames = new(),
            parentID = 0,
            parentStr = "",
            id = id
        };

        if (parNode.ValueKind == JsonValueKind.String)
            cache.parentStr = parNode.GetString();
        else
            cache.parentID = parNode.GetUInt32();

        foreach (JsonProperty property in objNode.EnumerateObject())
            if (!property.NameEquals("parentID") && !property.NameEquals("datablock"))
                cache.propertyNames.Add(property.Name);

        _caches.Enqueue(cache);
    }

    private static void RemoveInheritance_Internal(uint id) => InheritanceAPI<T>.GetRoot().RemoveNode(id);

    public void AddInheritance(T data)
    {
        if (!HasInheritance || !_caches.TryPeek(out var cache) || data.persistentID != cache.id) return;
        AddInheritance_Internal(data, _caches.Dequeue());
    }
    private static void AddInheritance_Internal(T data, InheritanceCache cache)
    {
        Type type = typeof(T);
        uint parentID = cache.parentID != 0 ? cache.parentID : PersistentIDManager.GetId(cache.parentStr);
        List<PropertyInfo> properties = new(cache.propertyNames.Count - 2);
        foreach (string name in cache.propertyNames)
        {
            PropertyInfo property = InheritanceAPI<T>.CacheProperty(type, name);
            if (property != null)
                properties.Add(property);
        }
        InheritanceAPI<T>.AddNode(cache.id, data, properties, parentID);
    }

    public void ApplyAllInheritance()
    {
        if (!HasInheritance) return;
        ApplyAllInheritance_Internal();
    }

    public static void ApplyAllInheritance_Internal()
    {
        InheritanceAPI<T>.ApplyAllInheritance();
    }
}