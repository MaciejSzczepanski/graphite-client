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
    public class WmiCounterNameProvider : ICounterNameProvider
    {
        const string WmiQuery = "select ProcessId, CommandLine from Win32_Process where Name='w3wp.exe'";
        readonly Dictionary<string, int> _processIdsByPoolName = new Dictionary<string, int>();
        readonly Dictionary<string, string> _instanceNameByPoolName = new Dictionary<string, string>();


        public virtual string GetCounterName(string poolName)
        {
            if (_instanceNameByPoolName.ContainsKey(poolName))
                return _instanceNameByPoolName[poolName];

            string instanceName = GetInstanceNameFromPerfcounter(GetProcessId(poolName));

            if (instanceName != null)
                _instanceNameByPoolName.Add(poolName, instanceName);

            return instanceName;
        }

        public void ReportInvalid(string appPoolName, string instanceName)
        {
            _processIdsByPoolName.Remove(appPoolName);
            _instanceNameByPoolName.Remove(appPoolName);
        }

        private void RefreshW3WpProcesses()
        {
            foreach (var pair in GetW3WpProcesses())
            {
                if (_processIdsByPoolName.ContainsKey(pair.Item1))
                    _processIdsByPoolName[pair.Item1] = pair.Item2;
                else
                    _processIdsByPoolName.Add(pair.Item1, pair.Item2);
            }
        }

        protected virtual IEnumerable<Tuple<string, int>> GetW3WpProcesses()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(WmiQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();
            foreach (var retObject in retObjectCollection)
            {
                var poolName = W3wpArgsParser.GetAppPoolName(retObject["CommandLine"].ToString());
                int pid = Convert.ToInt32(retObject["ProcessId"]);

                yield return Tuple.Create(poolName, pid);
            }
        }

        private int GetProcessId(string appPool)
        {
            if (!_processIdsByPoolName.ContainsKey(appPool))
            {
                RefreshW3WpProcesses();
            }

            return _processIdsByPoolName[appPool];
        }


        protected virtual string GetInstanceNameFromPerfcounter(int processId)
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
        private static readonly Regex Regex = new Regex(".+-ap\\s\"(?<poolName>\\w+)\".+", RegexOptions.Compiled);

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