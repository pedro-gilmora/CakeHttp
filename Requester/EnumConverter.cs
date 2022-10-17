using CakeHttp;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebHelpers
{
    public class EnumJsonConverter : JsonConverter<Enum>
    {
        internal EnumSerialization _enumSerialization;

        internal EnumJsonConverter(EnumSerialization enumSerialization) {
            _enumSerialization = enumSerialization;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }
        public override Enum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            if(GetSuitableValue(value, _enumSerialization) is { } val)
            {
                if (val is string str) 
                    writer.WriteStringValue(str);
                else 
                    writer.WriteNumberValue(Convert.ToUInt64(val));
            }
        }

        internal static object GetSuitableValue(Enum value, EnumSerialization _enumSerialization)
        {
            if (_enumSerialization.HasFlag(EnumSerialization.String) && value.ToString().Trim() is { } str)
            {
                return _enumSerialization switch
                {
                    EnumSerialization.CamelCaseString => JsonNamingPolicy.CamelCase.ConvertName(str),
                    EnumSerialization.UpperCaseString => str.ToUpperInvariant(),
                    EnumSerialization.LowerCaseString => str.ToLowerInvariant(),
                    _ => str
                };
            }
            return value;
        }
    }

}