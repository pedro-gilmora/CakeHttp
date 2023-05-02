using System;

namespace SourceCrafter.HttpServiceClient.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SegmentAttribute : Attribute
{
    public SegmentAttribute(string segment) { }
}
