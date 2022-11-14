using System.Text.Json;
using DevEverywhere.CakeHttp.Enums;
using System.Text.Json.Serialization;
using DevEverywhere.CakeHttp.Converters;
using DevEverywhere.CakeHttp.Inferfaces;

namespace DevEverywhere.CakeHttp.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public sealed class CakeHttpOptionsAttribute : Attribute, ICakeHttpInitOptions
{
    private bool _camelCasePathAndQuery;
    internal Func<string, string> _pathAndQueryTransformer;

    public string BaseUrl { get; }
    public PropertyCasing EnumSerialization { get; set; }
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
    public Dictionary<string, string> RequestContentHeaders { get; internal set; } = new ();

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    public CakeHttpOptionsAttribute(string baseUrl, bool camelCasePathAndQuery = false, PropertyCasing enumSerialization = PropertyCasing.CamelCase)
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

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    internal CakeHttpOptionsAttribute()
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AsQueryValueAttribute : Attribute { }
