using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HttPie.Enums;

namespace HttPie.Converters;

public class EnumJsonConverter : JsonConverter<Enum>
{
    internal Casing _enumSerialization;

    internal EnumJsonConverter(Casing enumSerialization)
    {
        _enumSerialization = enumSerialization;
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
        if (GetSuitableValue(value, _enumSerialization) is { } val)
        {
            if (val is string str)
                writer.WriteStringValue(str);
            else
                writer.WriteNumberValue(Convert.ToUInt64(val));
        }
    }

    internal static object GetSuitableValue(Enum value, Casing _enumSerialization)
    {
        if (value.ToString().Trim() is { } str)
        {
            return _enumSerialization switch
            {
                Casing.CamelCase => JsonNamingPolicy.CamelCase.ConvertName(str),
                Casing.UpperCase => str.ToUpperInvariant(),
                Casing.LowerCase => str.ToLowerInvariant(),
                Casing.PascalCase => str.Length > 0
                    ? (str[0].ToString().ToUpperInvariant() + (str.Length > 1 ? str.Substring(1) : "")).Replace("_", "")
                    : "",
                _ => str
            };
        }
        return value;
    }
}