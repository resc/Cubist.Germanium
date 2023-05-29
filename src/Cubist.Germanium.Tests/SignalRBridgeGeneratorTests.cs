using Cubist.Germanium.Generators.SignalRBridge;

namespace Cubist.Germanium.Tests;


[UsesVerify] // 👈 Adds hooks for Verify into XUnit
public class SignalRBridgeGeneratorTests
{
    [Fact]
    public Task GeneratesMessagesCorrectly()
    {
        // The source code to test
        var source = @" 
namespace Test;

public interface ITestClient
{
    Task SayHello(string name);
}

public interface ITestServer
{
    Task SayBye(string name);
}

public partial class TestHub : Hub<ITestClient>, ITestServer
{
    
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, new SignalRBridgeGenerator());
    }
}