using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Should.Fluent;
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
            var m = _counterNameProvider.Register("test", "w3wp#1");
            AppPoolListener listener = CreateAppPoolListener(m.PoolName, "Process", "Working Set");

            //when
            var value = listener.ReportValue();

            //then
            Assert.True(_counterFactory.CreatedCounters.All(p => p.CategoryName == listener.CategoryName && p.CounterName == listener.Counter && p.InstanceName == m.InstanceName));

        }

        [Fact]
        public void when_instancename_has_changed_apppool_listener_should_swich_to_new_perfcounter()
        {
            //given
            var m = _counterNameProvider.Register("test", "w3wp#1");
            
            var listener = CreateAppPoolListener(m.PoolName, "Process", "Working Set");

            var someValue = listener.ReportValue();

            //counter becomes invalid
            _counterFactory.CreatedCounters.Last().MarkAsInvalid();

            //pool is now in different process
            m = _counterNameProvider.Register("test", "w3wp#2");

            //when

            //on next read we should get null, as the counter failed
            var value2 = listener.ReportValue();
            value2.Should().Be.Null();

            //on next read counter should refresh and return values
            var value3 = listener.ReportValue();

            //then
            value3.Should().Not.Be.Null();

            listener.ReportValue();
            _counterNameProvider.WmiQueriesCount.Should().Equal(2);
        }


        [Fact]
        public void when_there_is_no_instanceName_for_a_pool_it_should_not_keep_on_scanning_system()
        {
            //given
            var listener = CreateAppPoolListener("test", "Process", "Working Set");

            //when
            for (int i= 0;i<10;i++)
            {
                listener.ReportValue();
            }
            

            //then
            _counterNameProvider.WmiQueriesCount.Should().Equal(1);
            _counterNameProvider.PerfcounterProcessScanCount.Should().Equal(0);
        }
    }
}
