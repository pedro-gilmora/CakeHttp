namespace HttPie.Enums;

public enum Casing : byte
{
    None = 0,
    CamelCase = 1,
    UpperCase = 2,
    LowerCase = 4,
    PascalCase = 8,
    LowerSnakeCase = 16,
    UpperSnakeCase = 32,
    Digit = 64
}