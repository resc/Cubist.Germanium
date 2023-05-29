using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cubist.Germanium.CSharp;

public static class TypeExtensions
{
    public static IReadOnlyDictionary<Type, string> KeywordTypes { get; } = new ConcurrentDictionary<Type, string>
    {
        [typeof(void)] = "void",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(char)] = "char",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(string)] = "string",
        [typeof(decimal)] = "decimal",
        [typeof(object)] = "object",
    };

    public static string CSharpName(this Type t)
    {
        if (t == null) return "";
        if (KeywordTypes.TryGetValue(t, out var keyword))
        {
            return keyword;
        }

        if (t.IsArray)
        {
            return t.GetElementType().CSharpName() + "[]";
        }

        if (t.IsGenericType)
        {
            var n = t.Name;
            n = n.Substring(0, n.IndexOf('`'));
            n = n + "<" + t.GetGenericArguments().Select(CSharpName).JoinWith(", ") + ">";
            return n;
        }

        return t.Name;

    }

    public static string CSharpFullName(this Type t)
    {
        if (t == null) return "";
        if (KeywordTypes.TryGetValue(t, out var keyword))
        {
            return keyword;
        }

        if (t.IsArray)
        {
            return t.GetElementType().CSharpFullName() + "[]";
        }

        if (t.IsGenericType)
        {
            var n = t.Name;
            n = n.Substring(0, n.IndexOf('`'));
            n = n + "<" + t.GetGenericArguments().Select(CSharpFullName).JoinWith(", ") + ">";
            return t.Namespace + Type.Delimiter + n;
        }
        else
        {
            return t.FullName ?? "";
        }
    }
}