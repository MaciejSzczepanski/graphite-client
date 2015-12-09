using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace Graphite.System
{
    public class WmiCounterNameProvider : ICounterNameProvider
    {
        const string WmiQuery = "select ProcessId, CommandLine from Win32_Process where Name='w3wp.exe'";
        readonly Dictionary<string, int> _processIdsByPoolName = new Dictionary<string, int>();
        readonly Dictionary<string, string> _instanceNameByPoolName = new Dictionary<string, string>();



        public string GetCounterName(string appPool)
        {
            if (_instanceNameByPoolName.ContainsKey(appPool))
                return _instanceNameByPoolName[appPool];

            string instanceName = ProcessNameById("w3wp", GetProcessId(appPool));

            if(instanceName != null)
                _instanceNameByPoolName.Add(appPool,instanceName);
            
            return instanceName;
        }

        private void RefreshW3wpProcesses()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(WmiQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();
            foreach (var o in retObjectCollection)
            {
                var retObject = (ManagementObject) o;

                var poolName = W3wpArgsParser.GetAppPoolName(retObject["CommandLine"].ToString());
                int pid = Convert.ToInt32(retObject["ProcessId"]);

                if (_processIdsByPoolName.ContainsKey(poolName))
                    _processIdsByPoolName[poolName] = pid;
                else
                    _processIdsByPoolName.Add(poolName, pid);
            }
        }

        private int GetProcessId(string appPool)
        {
            if (!_processIdsByPoolName.ContainsKey(appPool))
            {
                RefreshW3wpProcesses();
            }

            return _processIdsByPoolName[appPool];
        }


        private string ProcessNameById(string prefix, int processId)
        {
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