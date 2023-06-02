using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cubist.Germanium.CSharp;

internal sealed class TypeName : IEquatable<TypeName>
{
    private const string ArraySuffix = "[]";

    private static readonly ConcurrentDictionary<Type, TypeName> _typeNames;

    static TypeName()
    {
        _typeNames = new ConcurrentDictionary<Type, TypeName>(TypeExtensions.KeywordTypes.ToDictionary(x => x.Key, x => new TypeName("", x.Value)));

    }

    public static TypeName Var { get; } = new TypeName("", "var");

    public static IEnumerable<TypeName> For(params Type[] types)
    {
        return For((IEnumerable<Type>)types);
    }

    public static IEnumerable<TypeName> For(IEnumerable<Type> types)
    {
        foreach (var type in types)
            yield return Create(type);
    }

    public static TypeName Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("invalid name", nameof(fullName));

        // make a C# generic name 
        var backTickIndex = fullName.IndexOf('`');
        if (backTickIndex >= 0)
        {
            var genericParameterCount = int.Parse(fullName.Substring(backTickIndex + 1));
            var genericTypeParameters = Enumerable.Range(1, genericParameterCount).Select(n => $"T{n}");
            fullName = fullName.Substring(0, backTickIndex) + "<" + genericTypeParameters.JoinWith(", ") + ">";
        }

        // find the portion of the name before the angle brackets
        var angleBracketIndex = fullName.IndexOf('<');
        if (angleBracketIndex < 0)
            angleBracketIndex = fullName.Length;

        // find the last dot before the angle brackets
        var lastDot = fullName.LastIndexOf(Type.Delimiter, 0, angleBracketIndex);
        if (lastDot < 0)
        {
            return new TypeName("", fullName);
        }

        return new TypeName(fullName.Substring(0, lastDot), fullName.Substring(lastDot + 1));
    }

    public static TypeName Create(Type type)
    {
        return _typeNames.GetOrAdd(type, t =>
        {
            var ns = t.Namespace;
            var name = t.CSharpName();
            return new TypeName(ns, name);
        });
    }

    public TypeName(string ns, string name)
    {
        Name = name;
        if (string.IsNullOrWhiteSpace(ns))
        {
            FullName = name;
            Namespace = "";
        }
        else
        {
            FullName = ns + Type.Delimiter + name;
            Namespace = ns;
        }
    }

    public string Name { get; }

    public string Namespace { get; }

    public string FullName { get; }

    public bool IsArrayType => Name.EndsWith(ArraySuffix);

    public TypeName GetElementType()
    {
        if (!IsArrayType) throw new InvalidOperationException("Not an array type");
        return new TypeName(Namespace, Name.TrimSuffix(ArraySuffix));
    }

    public TypeName AsArrayType()
    {
        return new(Namespace, Name + ArraySuffix);
    }

    public bool Equals(TypeName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return FullName == other.FullName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TypeName)obj);
    }

    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }

    public static bool operator ==(TypeName left, TypeName right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypeName left, TypeName right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return FullName;
    }
}