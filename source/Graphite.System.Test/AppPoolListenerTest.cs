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

        public AppPoolListenerTest()
        {
            var appPoolProcess = Process.GetProcesses().Where(p => p.ProcessName.Equals("w3wp")).FirstOrDefault();
            Assert.True(appPoolProcess != null, "A runngin IIS appool is required to run this test");

            _counterFactory = new TestablePerformanceCounterFactory();
        }

        [Fact]
        public void ReportValue_Retrieves_a_value()
        {
            //given
            AppPoolListener listener = new AppPoolListener("test", "Process", "Working Set", new WmiCounterNameProvider(), _counterFactory);

            //when
            var value = listener.ReportValue();

            //then
            Assert.True(_counterFactory.CreatedCounters.All(p => p.CategoryName == "Process"));
           
        }
    }
}
