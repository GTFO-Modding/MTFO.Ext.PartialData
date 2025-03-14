using MTFO.Ext.PartialData.JsonConverters;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTFO.Ext.PartialData.Utils
{
    internal static class JSON
    {
        public readonly static JsonSerializerOptions Setting;
        public readonly static JsonSerializerOptions SettingWithoutInjectLib;

        static JSON()
        {
            Setting = CreateSetting();
            SettingWithoutInjectLib = CreateSetting();
        }

        private static JsonSerializerOptions CreateSetting()
        {
            var setting = new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                IncludeFields = true,
                AllowTrailingCommas = true,
                WriteIndented = true
            };

            setting.Converters.Add(new Il2CppListConverterFactory());
            setting.Converters.Add(new ColorConverter());
            setting.Converters.Add(new JsonStringEnumConverter());
            setting.Converters.Add(new LanguageDataConverter());
            setting.Converters.Add(new PersistentIDConverter());
            setting.Converters.Add(new LocalizedTextConverter());

            return setting;
        }

        public static T Deserialize<T>(string json, bool includeInjectLib = true)
        {
            return JsonSerializer.Deserialize<T>(json, includeInjectLib ? Setting : SettingWithoutInjectLib);
        }

        public static object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, Setting);
        }
    }
}