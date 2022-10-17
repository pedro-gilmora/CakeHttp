using System.Text.Json;
using WebHelpers;

namespace CakeHttp
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

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
        public CakeHttpOptionsAttribute(string baseUrl, bool camelCasePathAndQuery = false, EnumSerialization enumSerialization = EnumSerialization.CamelCaseString)
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
        {
            BaseUrl = baseUrl;
            CamelCasePathAndQuery = camelCasePathAndQuery;
            EnumSerialization = enumSerialization;
            if (Extensions.DefaultRestifyJsonOptions.Converters.OfType<EnumJsonConverter>().FirstOrDefault() is { _enumSerialization: { } _enumSerialization } cnv)
            {
                if(enumSerialization != _enumSerialization)
                    cnv._enumSerialization = enumSerialization;
            }
            else
            {
                Extensions.DefaultRestifyJsonOptions.Converters.Insert(0, new EnumJsonConverter(enumSerialization));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class AsQueryValueAttribute : Attribute { }
}
