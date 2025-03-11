using HarmonyLib;
using Localization;
using MTFO.Ext.PartialData.Utils;

namespace MTFO.Ext.PartialData.Injects;
[HarmonyPatch(typeof(CellSettingsApply), nameof(CellSettingsApply.ApplyLanguage))]
internal static class Inject_OnChangeLanguage
{
    static void Prefix(int value)
    {
        var language = (Language)value;
        TextDBUtil.FixNullLanguageData(language);
    }
}
