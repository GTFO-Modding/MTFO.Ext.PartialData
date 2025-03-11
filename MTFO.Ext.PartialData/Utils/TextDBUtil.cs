using GameData;
using HarmonyLib;
using Localization;
using System;

namespace MTFO.Ext.PartialData.Utils;
internal static class TextDBUtil
{
    public static void RefreshTranslation()
    {
        var currentLanguage = Text.TextLocalizationService.CurrentLanguage;
        var gdLocalization = Text.TextLocalizationService.Cast<GameDataTextLocalizationService>();
        gdLocalization.m_textDataBlocks = null;
        gdLocalization.m_texts.Clear();

        TextDataBlock[] allBlocks = GameDataBlockBase<TextDataBlock>.GetAllBlocks();
        gdLocalization.m_textDataBlocks = allBlocks;
        int count = allBlocks.Length;
        for (int i = 0; i < count; i++)
        {
            TextDataBlock textDataBlock = allBlocks[i];
            var text = textDataBlock.GetTextSafe(currentLanguage);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = textDataBlock.English;
            }
            gdLocalization.m_texts[textDataBlock.persistentID] = text;
        }

        Text.TextLocalizationService.SetCurrentLanguage(Text.TextLocalizationService.CurrentLanguage); //Update the TextDataBlock
        Text.UpdateAllTexts();
    }

    public static void FixNullLanguageData(Language currentLanguage)
    {
        if (!Enum.IsDefined(currentLanguage))
            return;

        if (currentLanguage == Language.English)
            return;

        var languageField = AccessTools.Property(typeof(TextDataBlock), currentLanguage.ToString());
        if (languageField == null)
            return;

        foreach (var block in TextDataBlock.GetAllBlocks())
        {
            var languageData = languageField.GetValue(block);
            if (languageData == null)
            {
                languageField.SetValue(block, new LanguageData(""));
            }
        }
    }

    public static string GetTextSafe(this TextDataBlock textDB, Language language)
    {
        LanguageData langData = language switch
        {
            Language.English => null,
            Language.French => textDB.French,
            Language.Italian => textDB.Italian,
            Language.German => textDB.German,
            Language.Spanish => textDB.Spanish,
            Language.Russian => textDB.Russian,
            Language.Portuguese_Brazil => textDB.Portuguese_Brazil,
            Language.Polish => textDB.Polish,
            Language.Japanese => textDB.Japanese,
            Language.Korean => textDB.Korean,
            Language.Chinese_Traditional => textDB.Chinese_Traditional,
            Language.Chinese_Simplified => textDB.Chinese_Simplified,
            _ => null,
        };

        if (langData == null)
            return textDB.English;
        else
            return langData.Translation;
    }
}
