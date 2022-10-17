using System.Text.Json;
using System.Text.Json.Serialization;
using CakeHttp.Converters;
using CakeHttp.Enums;
using DevEverywhere.CakeHttp;

namespace CakeHttp.Attributes
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class CakeHttpOptionsAttribute : Attribute, ICakeHttpInitOptions
    {
        private bool _camelCasePathAndQuery;
        internal Func<string, string> _pathAndQueryTransformer;

        public string BaseUrl { get; }
        public EnumSerialization EnumSerialization { get; set; }
        public bool CamelCasePathAndQuery
        {
            get => _camelCasePathAndQuery;
            set
            {
                _camelCasePathAndQuery = value;
                _pathAndQueryTransformer = _camelCasePathAndQuery ? JsonNamingPolicy.CamelCase.ConvertName : r => r;
            }
        }

        public Func<string, string> PathAndQueryFormatter => _pathAndQueryTransformer;

        public JsonSerializerOptions JsonOptions { get; }

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
        public CakeHttpOptionsAttribute(string baseUrl, bool camelCasePathAndQuery = false, EnumSerialization enumSerialization = EnumSerialization.CamelCaseString)
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
        {
            BaseUrl = baseUrl;
            CamelCasePathAndQuery = camelCasePathAndQuery;
            EnumSerialization = enumSerialization;

            JsonOptions = new(JsonSerializerDefaults.General) { 
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            JsonOptions.Converters.Insert(0, new EnumJsonConverter(enumSerialization));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class AsQueryValueAttribute : Attribute { }
}
