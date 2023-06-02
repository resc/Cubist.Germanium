using Cubist.Germanium.Generators.SignalRBridge;

namespace Cubist.Germanium.Tests;


[UsesVerify] // 👈 Adds hooks for Verify into XUnit
public class SignalRBridgeGeneratorTests
{
    [Fact]
    public Task GeneratesMessagesCorrectly()
    {
        // The source code to test
        var clientSource = """
            using System.Threading.Tasks;

            namespace Test;
            
            public interface ITestClient
            {
                Task SayHello(string name);
                Task SayCongratulation(string name, System.DateTime dateOfBirth);
            }
            """;

        var serverSource = """
            using System.Threading.Tasks;

            namespace Test;
            
            public interface ITestServer
            {
                Task SayHello(string name);
                Task SayBye(string name);
            }
            """;

        var hubSource = """ 
            using Microsoft.AspNetCore.SignalR;

            namespace Test;

            public partial class TestHub : Hub<ITestClient>, ITestServer
            {
                
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(new SignalRBridgeGenerator(), 
            (clientSource, $"{nameof(clientSource)}.cs"),
            (serverSource, $"{nameof(serverSource)}.cs"),
            (hubSource, $"{nameof(hubSource)}.cs"));
    }
}