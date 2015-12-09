using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Xunit;

namespace Graphite.Test
{
    public class ProcessTests
    {
        [Fact]
        public void GetCommandLine()
        {

            const string wmiQuery = "select ProcessId, CommandLine from Win32_Process where Name='w3wp.exe'";

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);

            for (int i = 0; i < 1000; i++)
            {
                //var processes = Process.GetProcessesByName("w3wp");

                
                
                ManagementObjectCollection retObjectCollection = searcher.Get();
                foreach (var o in retObjectCollection)
                {
                    var retObject = (ManagementObject) o;
                    Console.WriteLine("[ pid: {0} cmd: {1}]", retObject["ProcessId"], retObject["CommandLine"]);
                }
            }
           
            
        }

        [Fact]
        public void GetProcess()
        {
            for (int i = 0; i < 10; i++)
            {
              var p =  Process.GetProcessesByName("w3wp");
               
            }
            
        }

        [Fact]
        public void PercounterGet()
        {

          ProcessNameById("w3wp", 21128);
          
        }

        static Dictionary<long, string> _processNameById = new Dictionary<long, string>();
        private string ProcessNameById(string prefix, int processId)
        {
            if (_processNameById.ContainsKey(processId))
                return _processNameById[processId];

            var localCategory = new PerformanceCounterCategory("Process");

            string[] instances = localCategory.GetInstanceNames()
                .Where(p => p.StartsWith(prefix))
                .ToArray();

            foreach (string instance in instances)
            {
                using (var localCounter = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    long val = localCounter.RawValue;
                    
                    if (val == processId)
                    {
                        _processNameById.Add(val,instance);
                        return instance;
                    }
                }
            }

            return null;
        }
    }
}