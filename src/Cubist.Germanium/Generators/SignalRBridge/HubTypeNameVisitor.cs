using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cubist.Germanium.Generators.SignalRBridge;

/// <summary> Returns true if the node is a <see cref="GenericNameSyntax"/> with " </summary>
internal class GenericTypeNameVisitor : CSharpSyntaxVisitor<bool>
{
    private readonly string _name;

    public GenericTypeNameVisitor(string name)
    {
        _name = name;
    }

    public static GenericTypeNameVisitor Hub { get; } = new GenericTypeNameVisitor(nameof(Hub));

    public override bool VisitQualifiedName(QualifiedNameSyntax node)
    {
        Debugger.Launch();
        if (node.Right is GenericNameSyntax gns)
            return VisitGenericName(gns);
        return false;
    }

    public override bool VisitGenericName(GenericNameSyntax node)
    {
        var ident = node.Identifier.ValueText;
        return string.Equals(ident, _name, StringComparison.Ordinal);
    }
}