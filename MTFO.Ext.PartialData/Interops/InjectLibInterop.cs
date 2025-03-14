using BepInEx.Unity.IL2CPP;
using InjectLib.JsonNETInjection;
using InjectLib.JsonNETInjection.Supports;
using MTFO.Ext.PartialData.JsonConverters.InjectLibConverters;
using MTFO.Ext.PartialData.Utils;
using System;
using System.Runtime.CompilerServices;

namespace MTFO.Ext.PartialData.Interops
{
    internal static class InjectLibInterop
    {
        public const string PLUGIN_GUID = "GTFO.InjectLib";

        public static System.Text.Json.Serialization.JsonConverter InjectLibConnector { get; private set; } = null;

        public static bool IsLoaded { get; private set; } = false;

        internal static void Setup()
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
            {
                try
                {
                    IsLoaded = true;
                    SetupInjectLibSupports();
                }
                catch (Exception e)
                {
                    Logger.Error($"Exception thrown while reading data from GTFO.InjectLib: {e}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetupInjectLibSupports()
        {
            JsonInjector.SetConverter(new Il2CppPersistentIDConverter());
            JsonInjector.SetConverter(new Il2CppLocalizedTextConverter());

            InjectLibConnector = new InjectLibConnector();
            JSON.Setting.Converters.Add(InjectLibConnector);
        }
    }
}
