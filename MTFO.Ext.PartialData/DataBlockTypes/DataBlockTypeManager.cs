using GameData;
using Globals;
using HarmonyLib;
using LevelGeneration;
using MTFO.Ext.PartialData.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = MTFO.Ext.PartialData.Utils.Logger;

namespace MTFO.Ext.PartialData.DataBlockTypes
{
    internal static class DataBlockTypeManager
    {
        private readonly static List<IDataBlockType> _DataBlockCache = new List<IDataBlockType>();
        private readonly static List<IDBuffer> _DataBlockIdBuffers = new List<IDBuffer>();

        public static bool Initialize()
        {
            try
            {
                var dataBlockTypes = new List<Type>();
                var types = AccessTools.GetTypesFromAssembly(typeof(GameDataBlockBase<>).Assembly);
                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    if (string.IsNullOrEmpty(type.Namespace))
                        continue;

                    if (!type.Namespace.Equals("GameData"))
                        continue;

                    var baseType = type.BaseType;
                    if (baseType == null)
                        continue;

                    if (!baseType.Name.Equals("GameDataBlockBase`1"))
                    {
                        continue;
                    }

                    dataBlockTypes.Add(type);
                }

                var genericBaseType = typeof(DataBlockTypeWrapper<>);
                foreach (var type in dataBlockTypes)
                {
                    var genericType = genericBaseType.MakeGenericType(type);
                    var cache = (IDataBlockType)Activator.CreateInstance(genericType);
                    AssignForceChangeMethod(cache);
                    _DataBlockCache.Add(cache);
                    _DataBlockIdBuffers.Add(new IDBuffer());
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Can't make cache from Modules-ASM.dll!: {e}");
                return false;
            }
        }

        public static void AssignForceChangeMethod(IDataBlockType blockTypeCache)
        {
            //TODO: Better Support
            switch (blockTypeCache.GetShortName().ToLower())
            {
                case "rundown":
                    blockTypeCache.RegisterOnChangeEvent(() =>
                    {
                        var rundownPage = MainMenuGuiLayer.Current.PageRundownNew;
                        rundownPage.m_dataIsSetup = false;
                        try
                        {
                            clearIcon(rundownPage.m_expIconsTier1);
                            clearIcon(rundownPage.m_expIconsTier2);
                            clearIcon(rundownPage.m_expIconsTier3);
                            clearIcon(rundownPage.m_expIconsTier4);
                            clearIcon(rundownPage.m_expIconsTier5);
                            clearIcon(rundownPage.m_expIconsTierExt);

                            static void clearIcon(Il2CppSystem.Collections.Generic.List<CellMenu.CM_ExpeditionIcon_New> tier)
                            {
                                if (tier == null)
                                    return;


                                foreach (var icon in tier)
                                {
                                    var obj = icon.gameObject;
                                    if (obj != null)
                                        GameObject.Destroy(icon.gameObject);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"{e}");
                        }


                        rundownPage.m_currentRundownData = GameDataBlockBase<RundownDataBlock>.GetBlock(Global.RundownIdToLoad);
                        if (rundownPage.m_currentRundownData != null)
                        {
                            rundownPage.PlaceRundown(rundownPage.m_currentRundownData);
                            rundownPage.m_dataIsSetup = true;
                        }
                    });
                    break;

                case "fogsettings":
                    blockTypeCache.RegisterOnChangeEvent(() =>
                    {
                        if (!Builder.CurrentFloor.IsBuilt)
                        {
                            return;
                        }

                        var state = EnvironmentStateManager.Current.m_stateReplicator.State;
                        EnvironmentStateManager.Current.UpdateFogSettingsForState(state);
                    });
                    break;

                case "lightsettings":
                    blockTypeCache.RegisterOnChangeEvent(() =>
                    {
                        if (!Builder.CurrentFloor.IsBuilt)
                        {
                            return;
                        }

                        foreach (var zone in Builder.CurrentFloor.allZones)
                        {
                            foreach (var node in zone.m_courseNodes)
                            {
                                LG_BuildZoneLightsJob.ApplyLightSettings(0, node.m_lightsInNode, zone.m_lightSettings, false);
                            }
                        }
                    });
                    break;

                case "text":
                    blockTypeCache.RegisterOnChangeEvent(() =>
                    {
                        TextDBUtil.RefreshTranslation();
                    });
                    break;
            }
        }

        public static bool TryFindCache(string blockTypeName, out IDataBlockType cache)
        {
            var index = GetIndex(blockTypeName);
            if (index != -1)
            {
                cache = _DataBlockCache[index];
                return true;
            }

            cache = null;
            return false;
        }

        public static bool TryGetNextID(string blockTypeName, out uint id)
        {
            var index = GetIndex(blockTypeName);
            if (index != -1)
            {
                id = _DataBlockIdBuffers[index].GetNext();
                return true;
            }

            id = 0;
            return false;
        }

        public static void SetIDBuffer(string blockTypeName, uint id)
        {
            var index = GetIndex(blockTypeName);
            if (index != -1)
            {
                _DataBlockIdBuffers[index].CurrentID = id;
            }
        }

        public static void SetIDBuffer(string blockTypeName, uint id, IncrementMode mode)
        {
            var index = GetIndex(blockTypeName);
            if (index != -1)
            {
                var buffer = _DataBlockIdBuffers[index];
                buffer.CurrentID = id;
                buffer.IncrementMode = mode;
            }
        }

        private static int GetIndex(string blockTypeName)
        {
            blockTypeName = GetBlockName(blockTypeName);
            return _DataBlockCache.FindIndex(x => x.GetShortName().Equals(blockTypeName, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetBlockName(string blockTypeName)
        {
            blockTypeName = blockTypeName.Trim();
            if (blockTypeName.EndsWith("DataBlock"))
                blockTypeName = blockTypeName[0..^9];

            return blockTypeName;
        }
    }
}