using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Policy;

namespace SourceCrafter.HttpServiceClient.Converters;

public class EnumJsonConverter : JsonConverter<Enum>
{
    private CasingPolicy _enumSerialization;

    public EnumJsonConverter(Casing enumSerialization)
    {
        _enumSerialization = CasingPolicy.Create(enumSerialization);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }
    public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String && reader.GetString() is { } str)
            return Deserialize(typeToConvert, str);
        var num = reader.GetInt64();
        return Enum.GetValues(typeToConvert).Cast<Enum>().First(e => Convert.ToInt64(e).Equals(num));
    }

    private static Enum Deserialize(Type typeToConvert, string str)
    {
        return (Enum)Enum.Parse(typeToConvert, str, true);
    }

    public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
    {
        if (GetSuitableValue(value) is { } val)
        {
            if (val is string str)
                writer.WriteStringValue(str);
            else
                writer.WriteNumberValue(Convert.ToUInt64(val));
        }
    }

    internal object GetSuitableValue(Enum value)
    {
        if (value.ToString().Trim() is { } str)
            return _enumSerialization.ConvertName(str);

        return value;
    }
}