using System.Runtime.CompilerServices;

namespace Cubist.Germanium.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}