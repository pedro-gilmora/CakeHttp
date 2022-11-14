using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using HttPie.Enums;

namespace HttPie.CasingPolicies;

public class CasingPolicy : JsonNamingPolicy
{
    private readonly Casing _casing;

    private CasingPolicy(Casing casing) => _casing = casing;

    public static CasingPolicy Default(Casing casing) => new(casing);


    public override string ConvertName(string name)
    {
        if (name is not { Length: > 0 }) return name;
        return new string(FromChars(name, _casing).ToArray());

        static IEnumerable<char> FromChars(string name, Casing casing)
        {
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
                        yield return '_';
                    needUpper = casing is Casing.PascalCase or Casing.CamelCase;
                }
                else
                {
                    bool isUpper = char.IsUpper(ch);

                    if (start && char.IsLower(lastChar) && isUpper && casing is Casing.LowerCase or Casing.UpperCase || !start && char.IsDigit(ch))
                        yield return '_';

                    if ((needUpper || casing is Casing.UpperCase) && char.IsLower(ch))
                        yield return char.ToUpperInvariant(ch);
                    else if ((casing is Casing.LowerCase || !start && casing is Casing.CamelCase) && isUpper)
                        yield return char.ToLowerInvariant(ch);
                    else
                        yield return ch;

                    if (!start)
                        start = true;

                    needUpper = false;
                    lastChar = ch;
                }
            }
        }
    }
}