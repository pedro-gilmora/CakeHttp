using System;
using System.Runtime.CompilerServices;

namespace CakeHttp
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class FormDataAttribute : Attribute
    {
        public FormDataAttribute() { } 
        public FormDataAttribute(string key) {
            Key = key;
        }

        public string? Key { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class FormDataAttribute<T> : FormDataAttribute where T : HttpContent
    {
        public FormDataAttribute() { } 
        public FormDataAttribute(string key) {
            Key = key;
        }
        public Type Value { get; } = typeof(T);
    }
}