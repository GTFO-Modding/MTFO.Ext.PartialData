using AssetShards;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MTFO.Ext.PartialData.DataBlockTypes;
using MTFO.Ext.PartialData.Interops;
using MTFO.Ext.PartialData.Utils;
using System.IO;

namespace MTFO.Ext.PartialData
{
    [BepInPlugin("MTFO.Extension.PartialBlocks", "MTFO pDataBlock", "1.5.2")]
    [BepInProcess("GTFO.exe")]
    [BepInDependency(MTFOInterop.MTFOGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(InjectLibInterop.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(InheritanceInterop.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal class EntryPoint : BasePlugin
    {
        public static bool LogAddBlock = false;
        public static bool LogEditBlock = true;
        public static bool LogOfflineGearLink = false;
        public static bool LogInjectLibLink = false;
        public static bool LogDebugs = false;

        public override void Load()
        {
            Logger.LogInstance = Log;

            InheritanceInterop.Setup();
            InjectLibInterop.Setup();
            MTFOInterop.Setup();

            LogDebugs = Config.Bind(new ConfigDefinition("Logging", "Log Debug Messages"), false, new ConfigDescription("Using Debug Log Messages?")).Value;
            LogAddBlock = Config.Bind(new ConfigDefinition("Logging", "Log AddBlock"), false, new ConfigDescription("Using Log Message for AddBlock?")).Value;
            LogEditBlock = Config.Bind(new ConfigDefinition("Logging", "Log EditBlock"), true, new ConfigDescription("Using Log Message for Editing Block (Mostly by LiveEdit)?")).Value;
            LogOfflineGearLink = Config.Bind(new ConfigDefinition("Logging", "Log OfflineGear Links"), false, new ConfigDescription("Using Log Message for Linking GUID for OfflineGearJSON?")).Value;
            LogInjectLibLink = Config.Bind(new ConfigDefinition("Logging", "Log InjectLib Links"), false, new ConfigDescription("Using Log Message for Linking GUID for InjectLib?")).Value;

            var useLiveEdit = Config.Bind(new ConfigDefinition("Developer", "UseLiveEdit"), false, new ConfigDescription("Using Live Edit?"));
            PartialDataManager.CanLiveEdit = useLiveEdit.Value;

            if (!DataBlockTypeManager.Initialize())
            {
                Logger.Error("Unable to Initialize DataBlockTypeCache");
                return;
            }
            if (!PartialDataManager.Initialize())
            {
                Logger.Error("Unable to Initialize PartialData");
                return;
            }

            PersistentIDManager.DumpToFile(Path.Combine(PartialDataManager.PartialDataPath, "_persistentID.json"));
            AssetShardManager.add_OnStartupAssetsLoaded((Il2CppSystem.Action)OnAssetLoaded);

            var harmony = new Harmony("MTFO.pBlock.Harmony");
            harmony.PatchAll();
        }

        private bool once = false;

        private void OnAssetLoaded()
        {
            if (once)
                return;
            once = true;

            PartialDataManager.LoadPartialData();
            PartialDataManager.WriteAllFile(Path.Combine(MTFOInterop.GameDataPath, "CompiledPartialData"));

            TextDBUtil.RefreshTranslation();
        }
    }
}