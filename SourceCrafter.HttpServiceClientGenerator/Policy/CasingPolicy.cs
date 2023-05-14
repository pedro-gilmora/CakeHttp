#nullable enable
using System.Text.Json;
using SourceCrafter.HttpServiceClient.Enums;
using System;

namespace SourceCrafter.HttpServiceClient.Policies;

public class CasingPolicy : JsonNamingPolicy
{

    private readonly Func<object?, string?> _converter;

    private CasingPolicy(Casing casing) => _converter = GetConverter(casing);

    internal static Func<object?, string?> GetConverter(Casing casing)
    {
        return casing switch
        {
            Casing.CamelCase => r => $"{r}".ToCamel(),
            Casing.PascalCase => r => $"{r}".ToPascal(),
            Casing.LowerCase => r => $"{r}".ToLower(),
            Casing.UpperCase => r => $"{r}".ToUpper(),
            Casing.LowerSnakeCase => r => $"{r}".ToSnakeLower(),
            Casing.UpperSnakeCase => r => $"{r}".ToSnakeUpper(),
            Casing.Digit => r => r switch
            {
                Enum e => e.ToString("D"),
                _ => r?.ToString()
            },
            _ => r => $"{r}",
        };
    }

    public static CasingPolicy Create(Casing casing) => new(casing);

    public override string ConvertName(string name)
    {
        return _converter(name)!;
    }
}