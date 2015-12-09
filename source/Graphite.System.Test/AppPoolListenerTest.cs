using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Graphite.System.Test
{
    public class AppPoolListenerTest
    {
        private TestablePerformanceCounterFactory _counterFactory;
        private TestableWmiCounterNameProvider _counterNameProvider;

        public AppPoolListenerTest()
        {
            var appPoolProcess = Process.GetProcesses().Where(p => p.ProcessName.Equals("w3wp")).FirstOrDefault();
            Assert.True(appPoolProcess != null, "A runngin IIS appool is required to run this test");

            _counterFactory = new TestablePerformanceCounterFactory();
            _counterNameProvider = new TestableWmiCounterNameProvider();
        }

        private AppPoolListener CreateAppPoolListener(string poolName, string categoryName, string counterName)
        {
            return new AppPoolListener(poolName, categoryName, counterName, _counterNameProvider, _counterFactory);
        }

        [Fact]
        public void ReportValue_Retrieves_a_value()
        {
            //given
            var poolName = "test";
            var instanceName = "w3wp#1";
            _counterNameProvider.Register(poolName,instanceName);
            AppPoolListener listener = CreateAppPoolListener(poolName, "Process", "Working Set");

            //when
            var value = listener.ReportValue();

            //then
            Assert.True(_counterFactory.CreatedCounters.All(p => p.CategoryName == "Process" && p.InstanceName == instanceName));
           
        }
    }
}
