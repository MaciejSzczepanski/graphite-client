using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Graphite.System.Perfcounters;
using Xunit;

namespace Graphite.System.Test
{
    public class AppPoolListenerTest
    {
        public AppPoolListenerTest()
        {
            var appPoolProcess = Process.GetProcesses().Where(p => p.ProcessName.Equals("w3wp")).FirstOrDefault();
            Assert.True(appPoolProcess != null, "A runngin IIS appool is required to run this test");
        }

        [Fact]
        public void ReportValue_Retrieves_a_value()
        {
            //given
            AppPoolListener listener = new AppPoolListener("test", "Process", "Working Set", new WmiCounterNameProvider(), new PerformanceCounterFactory());

            //when
            var value = listener.ReportValue();

            //then
            Assert.True(value > 0f);
           
        }
    }
}
