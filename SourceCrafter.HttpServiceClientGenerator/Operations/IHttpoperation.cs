using SourceCrafter.HttpServiceClient.Operations;
using System;
using System.Collections.Generic;
using System.Text;
namespace SourceCrafter.HttpServiceClient.Operations
{
    public interface IHttpOperation
    {
    }

    public interface IHttpGet<T> where T : struct
    {
    }

    public interface IHttpPost<T> where T : struct
    {
    }

    public interface IHttpPut<T> where T : struct
    {
    }

    public interface IHttpDelete<T> where T : struct
    {
    }
    //[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false)]
    //public class HttpGetAttribute<T> : Attribute where T : struct
    //{
    //    public HttpGetAttribute(string queryParameterName = "query", string bodyParameterName = "body", string? segment = null) { }
    //}

    //[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false)]
    //public class HttpPostAttribute<T> : Attribute where T : struct
    //{
    //    public HttpPostAttribute(string queryParameterName = "query", string bodyParameterName = "body", string? segment = null) { }
    //}

    //[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false)]
    //public class HttpPutAttribute<T> : Attribute where T : struct
    //{
    //    public HttpPutAttribute(string queryParameterName = "query", string bodyParameterName = "body", string? segment = null) { }
    //}

    //[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false)]
    //public class HttpDeleteAttribute<T> : Attribute where T : struct
    //{
    //    public HttpDeleteAttribute(string queryParameterName = "query", string bodyParameterName = "body", string? segment = null) { }
    //}


    public interface IQueryParamName { }
    public interface IBodyParamName { }

    public readonly record struct Query<T>
    {
        private Query(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(Query<T> from) => from.Value;
        public static implicit operator Query<T>(T to) => new(to);
    }

    public readonly record struct RequestHeader<T>
    {
        private RequestHeader(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(RequestHeader<T> from) => from.Value;
        public static implicit operator RequestHeader<T>(T to) => new(to);
    }

    public readonly record struct ResponseHeader<T>
    {
        private ResponseHeader(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(ResponseHeader<T> from) => from.Value;
        public static implicit operator ResponseHeader<T>(T to) => new(to);
    }

    public readonly record struct RequestCookie<T>
    {
        private RequestCookie(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(RequestCookie<T> from) => from.Value;
        public static implicit operator RequestCookie<T>(T to) => new(to);
    }

    public readonly record struct ResponseCookie<T>
    {
        private ResponseCookie(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(ResponseCookie<T> from) => from.Value;
        public static implicit operator ResponseCookie<T>(T to) => new(to);
    }
    public readonly record struct Body<T>
    {
        private Body(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(Body<T> from) => from.Value;
        public static implicit operator Body<T>(T to) => new(to);
    }
    public readonly record struct FormUrlEncodedBody<T>
    {
        private FormUrlEncodedBody(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(FormUrlEncodedBody<T> from) => from.Value;
        public static implicit operator FormUrlEncodedBody<T>(T to) => new(to);
    }
    public readonly record struct FormBody<T>
    {
        private FormBody(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(FormBody<T> from) => from.Value;
        public static implicit operator FormBody<T>(T to) => new(to);
    }
    public readonly record struct JsonBody<T>
    {
        private JsonBody(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(JsonBody<T> from) => from.Value;
        public static implicit operator JsonBody<T>(T to) => new(to);
    }
    public readonly record struct Result<T>
    {
        private Result(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(Result<T> from) => from.Value;
        public static implicit operator Result<T>(T to) => new(to);
    }
    public readonly record struct JsonResult<T>
    {
        private JsonResult(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(JsonResult<T> from) => from.Value;
        public static implicit operator JsonResult<T>(T to) => new(to);
    }
    public readonly record struct JsonFail<T>
    {
        private JsonFail(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(JsonFail<T> from) => from.Value;
        public static implicit operator JsonFail<T>(T to) => new(to);
    }
    public readonly record struct XmlBody<T>
    {
        private XmlBody(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(XmlBody<T> from) => from.Value;
        public static implicit operator XmlBody<T>(T to) => new(to);
    }
    public readonly record struct XmlResult<T>
    {
        private XmlResult(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(XmlResult<T> from) => from.Value;
        public static implicit operator XmlResult<T>(T to) => new(to);
    }
    public readonly record struct XmlFail<T>
    {
        private XmlFail(T value) => Value = value;
        public T Value { get; }
        public static implicit operator T(XmlFail<T> from) => from.Value;
        public static implicit operator XmlFail<T>(T to) => new(to);
    }

#if DEBUG
    enum Status
    {
        Alive,
        Gone
    }
    partial interface IPet :
        IHttpDelete<(Query<Status>, JsonBody<bool>, Body<bool>)>
    {

    }
    class Test
    {
        void TestMethod()
        {
            int value = (Query<int>)1;
        }
    }
#endif
}
