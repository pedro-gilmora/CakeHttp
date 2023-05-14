#nullable enable
using System;
using System.Text;
using System.Collections;

namespace SourceCrafter.HttpServiceClient;

public sealed class QueryBuilder : IEnumerable
{
    private readonly StringBuilder _query = new();

    public static QueryBuilder StartWith(string key, string value)
    {
        return new()
        {
            {key, value}
        };
    }

    public QueryBuilder Add(string key, object? value)
    {
        if (value != null)
            _query.Append($"{(_query.Length > 0 ? "&" : "?")}{key}={Uri.EscapeDataString(value?.ToString())}");
        return this;
    }

    public override string ToString()
    {
        return _query.ToString();
    }

    public IEnumerator GetEnumerator()
    {
        return _query.ToString().GetEnumerator();
    }
}
