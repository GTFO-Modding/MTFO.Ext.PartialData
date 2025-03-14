using BepInEx.Unity.IL2CPP;

namespace MTFO.Ext.PartialData.Interops;
internal static class InheritanceInterop
{
    public const string PLUGIN_GUID = "Dinorush.InheritanceDataBlocks";

    public static bool IsLoaded { get; private set; } = false;

    internal static void Setup()
    {
        if (IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID))
        {
            IsLoaded = true;
        }
    }
}
