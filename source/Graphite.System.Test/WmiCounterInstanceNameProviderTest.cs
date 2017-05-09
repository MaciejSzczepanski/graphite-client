using System.Security;
using Xunit;

namespace Graphite.System.Test
{
    public class WmiCounterInstanceNameProviderTest
    {
        [Fact]
        public void GetW3WpProcesses_DoesNotThrow()
        {
            WmiCounterInstanceNameProvider provider = new WmiCounterInstanceNameProvider();

            var process = provider.GetW3WpProcesses();

            Assert.NotNull(process);
        }
    }
}