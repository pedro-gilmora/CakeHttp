using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using HttPie.Enums;
using System.Text;

namespace HttPie.Policy;

public class CasingPolicy : JsonNamingPolicy
{
    private readonly Casing _casing;

    private CasingPolicy(Casing casing) => _casing = casing;

    public static CasingPolicy Create(Casing casing) => new(casing);


    public override string ConvertName(string name)
    {
        if (name is not { Length: > 0 } && _casing is Casing.None) return name;
        return FromChars(name, _casing);
    }

    private static string FromChars(string name, Casing casing)
    {
        StringBuilder builder = new();
        int current = -1;
        int length = name.Length;
        bool start = false;
        bool needUpper = casing is Casing.UpperCase or Casing.PascalCase;
        char lastChar = char.MinValue;

        while (++current < length)
        {
            var ch = name[current];
            if (char.IsSeparator(ch))
            {
                if (!start) continue;

                if (casing is Casing.UpperCase or Casing.LowerCase)
                    builder.Append('_');

                needUpper = casing is Casing.PascalCase or Casing.CamelCase;
            }
            else
            {
                bool isUpper = char.IsUpper(ch);

                if (start && char.IsLower(lastChar) && isUpper && casing is Casing.LowerCase or Casing.UpperCase || !start && char.IsDigit(ch))
                    builder.Append('_');

                if ((needUpper || casing is Casing.UpperCase) && char.IsLower(ch))
                    builder.Append(char.ToUpperInvariant(ch));
                else if ((casing is Casing.LowerCase || !start && casing is Casing.CamelCase) && isUpper)
                    builder.Append(char.ToLowerInvariant(ch));
                else
                    builder.Append(ch);

                if (!start)
                    start = true;

                needUpper = false;
                lastChar = ch;
            }
        }

        return builder.ToString();
    }
}