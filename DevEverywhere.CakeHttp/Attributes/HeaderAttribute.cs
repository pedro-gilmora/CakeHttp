namespace CakeHttp.Attributes
{
    public abstract class HeaderBaseAttribute : Attribute
    {
        public abstract string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class HeaderAttribute : HeaderBaseAttribute
    {
        public HeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public override string Name { get; }
        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class HeaderResolverAttribute<TResolver> : HeaderBaseAttribute where TResolver : IValueResolver
    {
        public HeaderResolverAttribute(string name)
        {
            Name = name;
            Resolver = Activator.CreateInstance<TResolver>();
        }

        public override string Name { get; }
        public IValueResolver Resolver { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class HeaderAsyncResolverAttribute<TResolver> : HeaderBaseAttribute where TResolver : IAsyncValueResolver
    {
        public HeaderAsyncResolverAttribute(string name)
        {
            Name = name;
            Resolver = Activator.CreateInstance<TResolver>();
        }

        public override string Name { get; }
        public IAsyncValueResolver Resolver { get; }
    }

    public interface IValueResolver
    {
        string Resolve(string name);
    }

    public interface IAsyncValueResolver
    {
        Task<string> ResolveAsync(string name);
    }
}