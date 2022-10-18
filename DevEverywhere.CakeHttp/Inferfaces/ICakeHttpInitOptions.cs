using System.Text.Json;
using DevEverywhere.CakeHttp.Enums;

namespace DevEverywhere.CakeHttp.Inferfaces;

public interface ICakeHttpInitOptions
{
    string BaseUrl { get; }
    bool CamelCasePathAndQuery { get; set; }
    EnumSerialization EnumSerialization { get; set; }
    Func<string, string> PathAndQueryFormatter { get; }
    JsonSerializerOptions JsonOptions { get; }
}