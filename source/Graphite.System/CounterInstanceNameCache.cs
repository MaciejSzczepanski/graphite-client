using System;
using System.Collections.Generic;
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
}