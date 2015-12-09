using Xunit;

namespace Graphite.System.Test
{
    public class WmiCounterNameProviderTest
    {
        private const string AppPoolName = "DefaultAppPool";
        private WmiCounterNameProvider _provider;

        public WmiCounterNameProviderTest()
        {
            _provider = new WmiCounterNameProvider();
        }

        [Fact]
        public void GetCounterName()
        {
            //when
            string instanceName = _provider.GetCounterName(AppPoolName);

            //then
            Assert.Equal("w3wp", instanceName);
        }

        [Fact]
        public void GetCounterName_Gets_SameInstances()
        {
            //when
            string instanceName1 = _provider.GetCounterName(AppPoolName);
            string instanceName2 = _provider.GetCounterName(AppPoolName);

            //then
            Assert.Same(instanceName1,instanceName2);
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
    }
}