using System.Text.Json;
using SourceCrafter.HttpServiceClient.Enums;
using System.Text;
using System;
using SourceCrafter.HttpServiceClient;
using Microsoft.CodeAnalysis;

namespace SourceCrafter.HttpServiceClient.Policy;

public class CasingPolicy : JsonNamingPolicy
{

    private readonly Func<object, string> _converter;

    private CasingPolicy(Casing casing) => _converter = GetConverter(casing);

    internal static Func<object, string> GetConverter(Casing casing)
    {
        return casing switch
        {
            Casing.CamelCase => r => $"{r}".ToCamelCase(),
            Casing.PascalCase => r => $"{r}".ToPascalCase(),
            Casing.LowerCase => r => $"{r}".ToLower(),
            Casing.UpperCase => r => $"{r}".ToUpper(),
            Casing.LowerSnakeCase => r => $"{r}".ToLowerSnakeCase(),
            Casing.UpperSnakeCase => r => $"{r}".ToUpperSnakeCase(),
            Casing.Digit => r => r switch
            {
                Enum e => e.ToString("D"),
                object e => e.ToString(),
                _ => "0"
            },
            _ => r => $"{r}",
        };
    }

    public static CasingPolicy Create(Casing casing) => new(casing);

    public override string ConvertName(string name)
    {
        return _converter(name);
    }
}