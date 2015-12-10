using Should.Fluent;
using Xunit;

namespace Graphite.System.Test
{
    public class CounterInstanceNameCacheTest
    {
        private const string AppPoolName = "DefaultAppPool";
        private TestableCounterInstanceNameProvider _provider;
        private CounterInstanceNameCache _cache;

        public CounterInstanceNameCacheTest()
        {
            _provider = new TestableCounterInstanceNameProvider();
            _cache = new CounterInstanceNameCache(_provider);

        }

        [Fact]
        public void GetCounterName()
        {
            //given
             var m =_provider.Register(AppPoolName, "w3wp#1");

            //when
            string instanceName = _cache.GetCounterInstanceName(AppPoolName);

            //then
            Assert.Equal(m.InstanceName, instanceName);
        }

        [Fact]
        public void GetCounterName_Gets_SameInstances()
        {
            //given
            var m = _provider.Register(AppPoolName, "w3wp#1");

            //when
            string instanceName1 = _cache.GetCounterInstanceName(AppPoolName);
            string instanceName2 = _cache.GetCounterInstanceName(AppPoolName);

            //then
            Assert.Same(instanceName1,instanceName2);
            _provider.WmiQueriesCount.Should().Equal(1);
        }

        [Fact]
        public void W3wpArgsParser_gets_poolname_from_commandline_arguments_supplied_to_w3wp_process()
        {
            //given
            var testCmd =
                "c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \"DefaultAppPool\" -v \"v4.0\" -l \"webengine4.dll\" -a \\\\.\\pipe\\iisipm9a6cdc80-a844-4198-b361-d884bc7157a5 -h \"C:\\inetpub\\temp\\apppools\\DefaultAppPool\\DefaultAppPool.config\" -w \"\" -m 0 -t 20 -ta 0";

            //when
            string poolName = W3wpArgsParser.GetAppPoolName(testCmd);

            //then
            Assert.Equal("DefaultAppPool", poolName);
        }

        [Fact]
        public void GetCounterName_invalidates()
        {
            //given
            var m = _provider.Register(AppPoolName, "w3wp#1");

            //when
            string instanceName1 = _cache.GetCounterInstanceName(AppPoolName);

            _cache.ReportInvalid(AppPoolName, m.InstanceName);

            string instanceName2 = _cache.GetCounterInstanceName(AppPoolName);

            //then
            _provider.WmiQueriesCount.Should().Equal(2);
        }

    }
}