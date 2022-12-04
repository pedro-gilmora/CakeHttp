using System;

namespace HttPie.Generator.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SegmentAttribute : Attribute
{
    public SegmentAttribute(string segment) { }
}
