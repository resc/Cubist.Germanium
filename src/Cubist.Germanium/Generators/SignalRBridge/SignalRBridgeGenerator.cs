using System.Collections.Immutable;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using Cubist.Germanium.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cubist.Germanium.Generators.SignalRBridge;

/// <summary>  </summary>
[Generator]
public class SignalRBridgeGenerator : IIncrementalGenerator
{

    /// <summary>  </summary>
    public const string HubClassSyntaxProviderTrackingName = "HubClassSyntaxProvider";

    /// <summary>  </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hubClasses = context.SyntaxProvider
            .CreateSyntaxProvider(IsHubImplementation, GetHubClassInfo)
            .WithTrackingName(HubClassSyntaxProviderTrackingName)
            .Where(info => info != null)
            .Select((info, _) => info!)
            .Collect();

        context.RegisterSourceOutput(hubClasses, GenerateHubImplementation);
    }

    private bool IsHubImplementation(SyntaxNode node, CancellationToken cancellationToken)
    {
        // Do a preliminary check to see if we might have an interesting class declaration.
        // This is a fast check because this predicate will be called very often,
        // potentially on every key-stroke in the code editor.

        // check if we have a class declaration with 2 base types... 
        if (node is ClassDeclarationSyntax { BaseList.Types.Count: 2 } cds)
        {
            var typeSyntax = cds.BaseList.Types[0].Type;
            // and check if the first base type is a class named Hub
            return typeSyntax.Accept(GenericTypeNameVisitor.Hub);
        }
        return false;
    }

    private HubInfo? GetHubClassInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var hubImplSymbol = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);
        if (hubImplSymbol == null) return null;

        var hubImplType = GetTypeInfo(hubImplSymbol);

        // verify we've got a Hub<T> implementation
        var baseType = hubImplSymbol.BaseType;// SomeHub : Hub<T>, SomeInterface -> Hub<T>
        if (baseType == null) return null;
        // Verify the source assembly...
        if (!baseType.ContainingAssembly.Name.Equals("Microsoft.AspNetCore.SignalR.Core", StringComparison.Ordinal)) return null;
        // ... and the name of the base type ...
        if (!baseType.Name.Equals("Hub", StringComparison.Ordinal)) return null;
        // ... and the number of type parameters
        if (baseType.TypeParameters.Length != 1) return null;

        // verify the client interface
        if (baseType.TypeArguments[0] is not INamedTypeSymbol clientTypeSymbol) return null;

        // construct the client interface info
        var clientType = GetTypeInfo(clientTypeSymbol);
        var clientInterface = new InterfaceInfo(clientType, GetMethodInfo(clientType, clientTypeSymbol));

        // verify the server interface
        if (hubImplSymbol.Interfaces.Length != 1) return null;
        var serverTypeSymbol = hubImplSymbol.Interfaces[0]; // SomeHub : Hub<T>, SomeInterface -> SomeInterface

        // construct the server interface info
        var serverType = GetTypeInfo(serverTypeSymbol);
        var serverInterface = new InterfaceInfo(serverType, GetMethodInfo(serverType, serverTypeSymbol));

        // the hub info is complete, return it
        return new HubInfo(hubImplType, clientInterface, serverInterface);
    }

    private MethodInfo[] GetMethodInfo(TypeInfo declaringType, INamedTypeSymbol interfaceSymbol)
    {
        var methods = new List<MethodInfo>();
        foreach (var m in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var returnType = GetTypeInfo(m.ReturnType);
            var parameters = m.Parameters.Select(p => new ParameterInfo(p.Name, GetTypeInfo(p.Type))).ToArray();
            var method = new MethodInfo(m.Name, returnType, declaringType, parameters);
            methods.Add(method);
        }
        return methods.ToArray();
    }

    private static TypeInfo GetTypeInfo(ITypeSymbol typeSymbol, int recursionLevel = 0)
    {
        if (recursionLevel > 10) throw new InvalidOperationException($"Generic type recursion on type {typeSymbol.ToDisplayString()}");
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            try
            {
                var typeParameters = namedTypeSymbol.TypeParameters
                    .Select(t => GetTypeInfo(t, recursionLevel + 1))
                    .ToArray();
                return new TypeInfo(namedTypeSymbol.ContainingNamespace.ToDisplayString(), namedTypeSymbol.Name, typeParameters);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(e.Message + " <- " + typeSymbol.ToDisplayString(), e);
            }
        }
        return new(typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.Name);
    }

    private void GenerateHubImplementation(SourceProductionContext context, ImmutableArray<HubInfo> hubs)
    {
        var source = new CodeWriter();
        source.WriteComment("<auto-generated />");
        source.WriteComment($"{hubs.Length} hub{(hubs.Length == 1 ? "" : "s")} found");
        source.WriteLine();

        foreach (var hub in hubs)
        {
            GenerateHubImplementation(context, source, hub);
        }

        context.AddSource($"{nameof(SignalRBridgeGenerator)}.g.cs", source.ToString());
    }

    private void GenerateHubImplementation(SourceProductionContext _, CodeWriter source, HubInfo hub)
    {
        source.WriteLine();

        using (source.Namespace(hub.Type.Namespace.TrimPrefix("global::")))
        {
            source.WriteLine($"public partial class {hub.Type.Name}");
            using (source.Block())
            {
                source.WriteLine(@$"public const string HubDispatcherActorSelection = ""/user/signalr/{hub.Type.Name.ToLowerInvariant()}"";");
                source.WriteLine();

                source.WriteLine($"private readonly global::Akka.Actor.ActorSystem _actorSystem;");
                source.WriteLine();
                source.WriteLine($"public {hub.Type.Name}(global::Akka.Actor.ActorSystem actorSystem)");
                using (source.Block())
                {
                    source.WriteLine("_actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));");
                }
                source.WriteLine();

                WriteInterfaceImplementation(source, hub, hub.ServerInterface);

                source.WriteLine($"public static class ToClient");
                using (source.Block())
                {
                    WriteMessages(source, MessageDirection.ToClient, hub.ClientInterface);
                }
                source.WriteLine();
                source.WriteLine();
                source.WriteLine($"public static class ToServer");
                using (source.Block())
                {
                    WriteMessages(source, MessageDirection.ToServer, hub.ServerInterface);
                }
                source.WriteLine();
                source.WriteLine();


            }
        }
    }

    private void WriteInterfaceImplementation(CodeWriter source, HubInfo hub, InterfaceInfo interfaceInfo)
    {
        foreach (var method in interfaceInfo.Methods)
        {
            WriteMethodImpl(source, hub, method);
        }
    }

    private void WriteMethodImpl(CodeWriter source, HubInfo _, MethodInfo method)
    {
        source.Write($"public global::{method.ReturnType.Namespace}.{method.ReturnType.Name} {method.Name}(");
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0) source.Write(", ");
            var p = method.Parameters[i];
            source.Write($"{p.Type.ToGlobalName()} {p.Name}");
        }
        source.WriteLine(")");
        using (source.Block())
        {
            source.Write($"var msg = new ToServer.{method.Name}(");
            source.Write("Context.ConnectionId, ");
            source.Write("Context.UserIdentifier, ");
            source.Write("Context.User ");

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                source.Write(", ");
                var p = method.Parameters[i];
                source.Write($"{p.Name}");
            }
            source.WriteLine(");");

            source.WriteLine(@$"_actorSystem.ActorSelection(HubDispatcherActorSelection).Tell(msg);");
        }
        source.WriteLine();
    }

    private void WriteMessages(CodeWriter source, MessageDirection direction, InterfaceInfo interfaceInfo)
    {
        foreach (var method in interfaceInfo.Methods)
        {
            WriteMessage(source, direction, method);
            source.WriteLine();
        }
    }

    private void WriteMessage(CodeWriter source, MessageDirection direction, MethodInfo method)
    {
        using (source.XmlDoc())
        {
            using (source.Summary())
            {
                source.See(method.Name);
                source.WriteLine(" is derived from ");
                source.See(method.DeclaringType.ToGlobalName() + "." + method.Name + "(" +
                           string.Join(", ", method.Parameters.Select(p => p.Type.ToGlobalName())) + ")");
            }
        }

        source.Write($"public record {method.Name}(");
        switch (direction)
        {
            case MessageDirection.ToClient:
                source.Write($"string ConnectionId");
                break;
            case MessageDirection.ToServer:
                source.Write($"string ConnectionId, ");
                source.Write($"string UserIdentifier, ");
                source.Write($"global::System.Security.Claims.ClaimsPrincipal User");
                break;
        }
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            source.Write(", ");
            var p = method.Parameters[i];
            source.Write($"{p.Type.ToGlobalName()} {p.Name.ToPascalCase()}");
        }
        source.WriteLine(")");

        using (source.Block())
        {

            var hubClients = "hubClients";
            var hubClientsParam = "global::Microsoft.AspNetCore.SignalR.IHubClients<" + method.DeclaringType.ToGlobalName() + "> " + hubClients;
            var methodArgs = string.Join(", ", method.Parameters.Select(p => p.Name.ToPascalCase()));
            var returnType = method.ReturnType.ToGlobalName();
            source.WriteLine($"public {returnType} CallOnAll({hubClientsParam})\n{source.IndentText}=> {hubClients}.All.{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnAllExcept({hubClientsParam}, global::System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds)\n{source.IndentText}=> {hubClients}.AllExcept(excludedConnectionIds).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnClient({hubClientsParam}, string connectionId)\n{source.IndentText}=> {hubClients}.Client(connectionId).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnClients({hubClientsParam}, global::System.Collections.Generic.IReadOnlyList<string> connectionIds)\n{source.IndentText}=> {hubClients}.Clients(connectionIds).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnGroup({hubClientsParam}, string groupName)\n{source.IndentText}=> {hubClients}.Group(groupName).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnGroups({hubClientsParam}, global::System.Collections.Generic.IReadOnlyList<string> groupNames)\n{source.IndentText}=> {hubClients}.Groups(groupNames).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnGroupExcept({hubClientsParam}, string groupName, global::System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds)\n{source.IndentText}=> {hubClients}.GroupExcept(groupName, excludedConnectionIds).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnUser({hubClientsParam}, string userId)\n{source.IndentText}=> {hubClients}.User(userId).{method.Name}({methodArgs});\n");
            source.WriteLine($"public {returnType} CallOnUsers({hubClientsParam}, global::System.Collections.Generic.IReadOnlyList<string> userIds)\n{source.IndentText}=> {hubClients}.Users(userIds).{method.Name}({methodArgs});\n");
        }

    }

    private enum MessageDirection
    {
        ToClient,
        ToServer
    }

    private enum ResponseType
    {
        All,
        AllExcept,
        Clients,
        Groups,
        GroupsExcept,
        Users
    }
}


record HubInfo(TypeInfo Type, InterfaceInfo ClientInterface, InterfaceInfo ServerInterface);


record InterfaceInfo(TypeInfo Type, params MethodInfo[] Methods);

record MethodInfo(string Name, TypeInfo ReturnType, TypeInfo DeclaringType, params ParameterInfo[] Parameters);

record ParameterInfo(string Name, TypeInfo Type);

/// <summary> constructs  generic type info </summary> 
record TypeInfo(string Namespace, string Name, bool IsGeneric, params TypeInfo[] TypeParameters)
{
    /// <summary> constructs non-generic type info </summary> 
    public TypeInfo(string ns, string name, params TypeInfo[] typeParameters) : this(ns, name, typeParameters.Length > 0, typeParameters) { }
}

static class InfoExtensions
{
    public static string ToGlobalName(this TypeInfo typeInfo)
    {
        if (typeInfo.IsGeneric)
        {
            return $"global::{typeInfo.Namespace}.{typeInfo.Name}<{string.Join(", ", typeInfo.TypeParameters.Select(ToGlobalName))}>";
        }

        return $"global::{typeInfo.Namespace}.{typeInfo.Name}";
    }
}
