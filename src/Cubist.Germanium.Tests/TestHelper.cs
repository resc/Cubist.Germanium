using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp; 

namespace Cubist.Germanium.Tests;
/// <summary>
/// See https://andrewlock.net/creating-a-source-generator-part-2-testing-an-incremental-generator-with-snapshot-testing/
/// </summary>
public static class TestHelper
{
    public static Task Verify(  IIncrementalGenerator generator, params (string source,string name)[] sources)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTrees = sources.Select(x => CSharpSyntaxTree.ParseText(x.source, path: x.name)).ToArray();
        // Create references for assemblies we require
        // We could add multiple references if required
        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Hub<>).Assembly.Location)
        };
        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: references); // 👈 pass the references to the compilation
        
        // The GeneratorDriver is used to run our generator against a compilation
        var csdriver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        var driver = csdriver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }
}