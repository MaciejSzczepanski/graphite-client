using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Graphite.System.Perfcounters;

namespace Graphite.System
{
    public class WmiCounterInstanceNameProvider : ICounterInstanceNameProvider
    {
        const string WmiQuery = "select ProcessId, CommandLine from Win32_Process where Name='w3wp.exe'";

        public virtual IEnumerable<Tuple<string, int>> GetW3WpProcesses()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(WmiQuery))
            {
                ManagementObjectCollection retObjectCollection = searcher.Get();
                foreach (var retObject in retObjectCollection)
                {
                    var poolName = W3wpArgsParser.GetAppPoolName(retObject["CommandLine"].ToString());
                    int pid = Convert.ToInt32(retObject["ProcessId"]);

                    yield return Tuple.Create(poolName, pid);
                }
            }
        }
        
        public virtual string GetInstanceName(int processId)
        {
            var localCategory = new PerformanceCounterCategory("Process");

            string[] instances = localCategory.GetInstanceNames()
                .Where(p => p.StartsWith("w3wp"))
                .ToArray();

            foreach (string instance in instances)
            {
                using (var localCounter = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    long val = localCounter.RawValue;

                    if (val == processId)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }
    }

    public static class W3wpArgsParser
    {
        //  private static readonly Regex Regex = new Regex(".+-ap\\s\"(?<poolName>[a-zA-Z_0-9\\-\\s\\.]+)\".+", RegexOptions.Compiled);
        private static readonly Regex Regex = new Regex(".+-ap\\s\"(?<poolName>.+?)\".+", RegexOptions.Compiled);

        public static string GetAppPoolName(string cmd)
        {
            if (Regex.IsMatch(cmd))
            {
                return Regex.Match(cmd).Groups["poolName"].Value;
            }

            return null;
        }
    }
}