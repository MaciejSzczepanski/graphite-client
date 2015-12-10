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
    public class CounterInstanceNameCache
    {
        private readonly ICounterInstanceNameProvider _instanceNameProvider;
        readonly Dictionary<string, int?> _processIdsByPoolName = new Dictionary<string, int?>();
        readonly Dictionary<string, string> _instanceNameByPoolName = new Dictionary<string, string>();
        public CounterInstanceNameCache(ICounterInstanceNameProvider instanceNameProvider)
        {
            _instanceNameProvider = instanceNameProvider;
            this.Expiration = TimeSpan.MaxValue;
        }

        public TimeSpan Expiration { get; set; }
        DateTime _lastProcessListRefresh;

        private bool HasExpired()
        {
            if (this.Expiration == TimeSpan.MaxValue)
                return false;

            return _lastProcessListRefresh.Add(Expiration) <= DateTime.UtcNow;
        }

        public virtual string GetCounterInstanceName(string poolName)
        {
            if (HasExpired())
                Invalidate();
            
            if (_instanceNameByPoolName.ContainsKey(poolName))
                return _instanceNameByPoolName[poolName];


            var processId = GetProcessId(poolName);
            string instanceName = null;

            if (processId.HasValue)
                instanceName = _instanceNameProvider.GetInstanceName(processId.Value);

            _instanceNameByPoolName.Add(poolName, instanceName);

            return instanceName;
        }

        private int? GetProcessId(string appPool)
        {
          

            if (!_processIdsByPoolName.ContainsKey(appPool))
            {
                RefreshW3WpProcesses();
            }

            if (!_processIdsByPoolName.ContainsKey(appPool))
                _processIdsByPoolName.Add(appPool, null);

            return _processIdsByPoolName[appPool];
        }

        

        public void ReportInvalid(string appPoolName, string instanceName)
        {
            _processIdsByPoolName.Remove(appPoolName);
            _instanceNameByPoolName.Remove(appPoolName);
        }

        public void Invalidate()
        {
            _processIdsByPoolName.Clear();
            _instanceNameByPoolName.Clear();
        }

        private void RefreshW3WpProcesses()
        {
            foreach (var pair in _instanceNameProvider.GetW3WpProcesses())
            {
                if (_processIdsByPoolName.ContainsKey(pair.Item1))
                    _processIdsByPoolName[pair.Item1] = pair.Item2;
                else
                    _processIdsByPoolName.Add(pair.Item1, pair.Item2);

                _lastProcessListRefresh = DateTime.UtcNow;

            }
        }
    }

    public class WmiCounterInstanceNameProvider : ICounterInstanceNameProvider
    {
        const string WmiQuery = "select ProcessId, CommandLine from Win32_Process where Name='w3wp.exe'";

        public virtual IEnumerable<Tuple<string, int>> GetW3WpProcesses()
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