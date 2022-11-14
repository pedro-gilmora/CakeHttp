using System;

namespace HttPie.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AsQueryValueAttribute : Attribute { }