using System.Text.Json;
using HttPie.Enums;
using System.Text;
using System;
using HttPie.Generator;
using Microsoft.CodeAnalysis;

namespace HttPie.Policy;

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

    internal static string GetConverterExpression(string value, ITypeSymbol type, Casing propCasing)
    {
        switch (type)
        {
            case INamedTypeSymbol { EnumUnderlyingType: not null }:
                return value + propCasing switch
                {
                    Casing.Digit => @".ToString(""D"")",
                    Casing.CamelCase => ".ToCamelCase()",
                    Casing.PascalCase => ".ToPascalCase()",
                    Casing.LowerCase => ".ToLower()",
                    Casing.UpperCase => ".ToUpper()",
                    Casing.LowerSnakeCase => ".ToLowerSnakeCase()",
                    Casing.UpperSnakeCase => ".ToUpperSnakeCase()",
                    _ => ""
                };
            case not { SpecialType: SpecialType.System_String }:
                return value + ".ToString()";
        }

        return "";
    }

    public static CasingPolicy Create(Casing casing) => new(casing);

    public override string ConvertName(string name)
    {
        return _converter(name);
    }
}