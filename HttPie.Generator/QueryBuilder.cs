using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HttPie.Generator;

public sealed class QueryBuilder : IEnumerable
{
    private readonly StringBuilder query = new();

    public static QueryBuilder StartWith(string key, string value)
    {
        return new()
        {
            {key, value}
        };
    }

    public static QueryBuilder<T> With<T>(T obj)
    {
        QueryBuilder<T> result = new(obj);
        return result;
    }

    public QueryBuilder Add(string key, string value)
    {
        query.Append($"{(query.Length > 0 ? "&" : "?")}{key}={value}");
        return this;
    }

    public override string ToString()
    {
        return query.ToString();
    }

    public IEnumerator GetEnumerator()
    {
        return query.ToString().GetEnumerator();
    }
}

public sealed class QueryBuilder<T> : IEnumerable
{
    private readonly T _obj;
    private readonly StringBuilder _query = new();

    internal QueryBuilder(T obj)
    {
        _obj = obj;
    }

    public QueryBuilder<T> Add(string key, string value)
    {
        _query.Append($"{(_query.Length > 0 ? "&" : "?")}{key}={value}");
        return this;
    }

    public QueryBuilder<T> Add<TIn>(string key, Func<T, TIn> valueGetter)
    {
        if (valueGetter(_obj) is { } val)
            _query.Append($"{(_query.Length > 0 ? "&" : "?")}{key}={val}");
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
