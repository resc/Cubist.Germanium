using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubist.Germanium.CSharp;

internal static class CSharpCodeWriteExtensions
{

    public static Scope Namespace(this CodeWriter cw, string ns)
    {
        cw.WriteLine($"namespace {ns}");
        return cw.Block();
    }
    public static void NamespaceStmt(this CodeWriter cw, string ns)
    {
        cw.WriteLine($"namespace {ns};"); 
    }

    public static Scope XmlDoc(this CodeWriter cw)
    {
        return cw.UseLinePrefix("/// ");
    }
    public static Scope Summary(this CodeWriter cw)
    {
        cw.WriteLine("<summary>");
        return Scope.Create(() => cw.WriteLine("</summary>"));
    }

    public static void See(this CodeWriter cw, string typeref)
    {
        cw.Write($"<see cref=\"{typeref}\">");

    }

    public static void UsingFor(this CodeWriter cw, params TypeName[] types)
    {
        cw.UsingFor((IEnumerable<TypeName>)types);
    }

    public static void UsingFor(this CodeWriter cw, IEnumerable<TypeName> types)
    {
        var nss = types
            .Select(x => x.Namespace)
            .Select(x => string.IsNullOrWhiteSpace(x) ? "System" : x)
            .Distinct()
            .OrderBy(x => "System".Equals(x) || x.StartsWith("System.") ? 0 : 1)
            .ThenBy(x => x);

        foreach (var ns in nss)
            cw.Using(ns);
    }

    public static void UsingFor(this CodeWriter cw, IEnumerable<Type> types)
    {
        cw.UsingFor(types.Select(TypeName.Create));
    }

    public static void UsingFor(this CodeWriter cw, params Type[] types)
    {
        cw.UsingFor(types.Select(TypeName.Create));
    }

    public static void Using(this CodeWriter cw, string ns)
    {
        cw.WriteLine($"using {ns};");
    }

    public static void UsingStatic(this CodeWriter cw, string classname)
    {
        cw.WriteLine($"using static {classname};");
    }

    public static Scope Class(this CodeWriter cw, string name, params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            cw.Write(kw);
            cw.Write(" ");
        }
        cw.Write("class ");
        cw.WriteLine(name);
        return cw.Block();

    }
}