using GameData;
using InjectLib.JsonNETInjection.Supports;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTFO.Ext.PartialData.JsonConverters;
internal class InjectLibConnectorWrapper : JsonConverterFactory

{
    private static readonly InjectLibConnector _Connector = new();

    public override bool CanConvert(Type typeToConvert)
    {
        Type baseType = typeToConvert.BaseType;

        // For some reason, grouping the inheritance check with the GetGeneric... runs the latter when it shouldn't
        if (!baseType.IsGenericType)
            return _Connector.CanConvert(typeToConvert);

        // Cannot let datablocks be taken to InjectLib land, lest the converters fail to apply.
        if (baseType.GetGenericTypeDefinition() == typeof(GameDataBlockBase<>))
            return false;

        return _Connector.CanConvert(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return _Connector.CreateConverter(typeToConvert, options);
    }
}
