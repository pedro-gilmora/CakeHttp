using SourceCrafter.HttpServiceClient.Enums;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceCrafter.HttpServiceClientGenerator.Internals
{
    internal enum ObjectKind { Class, Interface }
    //internal sealed record FileDescriptor
    //{
    //    internal ImmutableHashSet<string> Usings { get; set; } = ImmutableHashSet<string>.Empty;

    //    //public override string ToString()
    //    //{
    //    //    StringBuilder builder = new("");
    //    //    foreach
    //    //    return builder.ToString();
    //    //}
    //}

    internal sealed record TypeDescriptor
    {
        internal ObjectKind Kind { get; set; } = ObjectKind.Class;
        internal string Name { get; set; } = null!;
        internal string Namespace { get; set; } = null!;
        internal string Initializer { get; set; } = null!;
        internal string Modifier { get; set; } = null!;
        internal ImmutableHashSet<string> Usings { get; set; } = ImmutableHashSet<string>.Empty;
        internal ImmutableHashSet<TypeDescriptor> OtherTypes { get; set; } = ImmutableHashSet<TypeDescriptor>.Empty;
        internal ImmutableHashSet<string> Base { get; set; } = ImmutableHashSet<string>.Empty;
        internal ImmutableHashSet<ObjectMember> Members { get; set; } = ImmutableHashSet<ObjectMember>.Empty;
        public void ToString(StringBuilder builder)
        {
            builder.Append(@$"
namespace {Namespace}
{{
    {Modifier} class {Name} : {Base}
    {{{Initializer}");

            foreach (ObjectMember member in Members)
                member.ToString(builder);

            builder.Append(@$"
    }}
}}");
            foreach (var item in OtherTypes)
                item.ToString(builder);

        }
    }
    internal enum MemberType { Method, Property, Indexer, Field }
    internal sealed record ObjectMember
    {
        internal string Name { get; set; } = null!;
        internal MemberType Type { get; set; }
        internal string? Modifiers { get; set; }
        internal CommaSeparatedSyntax Parameters { get; set; } = new();
        internal string Return { get; set; } = null!;
        internal string Body { get; set; } = null!;

        internal void ToString(StringBuilder builder)
        {
            builder.Append(@"
        ");
            if (Modifiers != null)
                builder.Append($"{Modifiers} ");

            builder.Append($@"{Return} {Name}");

            if (Parameters is { Expressions.Count: > 0 } p)
            {
                p.WrapperSymbol = Type == MemberType.Indexer ? "[]" : "()";
                builder.Append(p);
            }

            builder.Append($@"{Body}");
        }
    }
}
