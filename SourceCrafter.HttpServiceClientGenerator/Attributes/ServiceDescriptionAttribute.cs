using System;

namespace SourceCrafter.HttpServiceClient.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ServiceDescriptionAttribute : Attribute
{
#pragma warning disable IDE0060 // Quitar el parámetro no utilizado
    public ServiceDescriptionAttribute(string? serviceName = null, string? segment = null) { }
#pragma warning restore IDE0060 // Quitar el parámetro no utilizado
}
