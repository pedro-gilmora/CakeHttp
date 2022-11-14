#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace HttPie.Generator
{
    public static class Extensions
    {
        public static string Join<T>(this IEnumerable<T> strs, Func<T, string> formmater, string? separator = "")
        {
            return string.Join(separator, strs?.Select(formmater) ?? Enumerable.Empty<string>());
        }
        public static string Join<T>(this IEnumerable<T> strs, string? separator = "")
        {
            return Join(strs, t => t != null ? t.ToString() : "", separator);
        }
    }
}
