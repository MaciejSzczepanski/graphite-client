using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Graphite.System.Perfcounters;
using NLog;

namespace Graphite.System
{
    public class CounterInstanceNameCache
    {
        private readonly ICounterInstanceNameProvider _instanceNameProvider;
        private object _lock = new object();
        readonly ConcurrentDictionary<string, int?> _processIdsByPoolName = new ConcurrentDictionary<string, int?>();
        readonly ConcurrentDictionary<string, string> _instanceNameByPoolName = new ConcurrentDictionary<string, string>();

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

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

            string instanceName = null;
            if (_instanceNameByPoolName.TryGetValue(poolName, out instanceName))
                return instanceName;
            
            lock (_lock)
            {
                if (_instanceNameByPoolName.ContainsKey(poolName))
                    return _instanceNameByPoolName[poolName];

                var processId = GetProcessId(poolName);
                

                if (processId.HasValue)
                    instanceName = _instanceNameProvider.GetInstanceName(processId.Value);

                _instanceNameByPoolName.AddOrUpdate(poolName, instanceName, (key, existingvalue) => instanceName);

                return instanceName;
            }
          
        }

        private int? GetProcessId(string appPool)
        {
            if (!_processIdsByPoolName.ContainsKey(appPool))
            {
                RefreshW3WpProcesses();
            }

            if (!_processIdsByPoolName.ContainsKey(appPool))
                _processIdsByPoolName.AddOrUpdate(appPool, (int?) null, (k,v) => (int?) null);

            return _processIdsByPoolName[appPool];
        }


        public void ReportInvalid(string appPoolName, string instanceName)
        {
            lock (_lock)
            {
                int? value;
                _processIdsByPoolName.TryRemove(appPoolName, out value);
                string removedInstanceName;
                _instanceNameByPoolName.TryRemove(appPoolName, out removedInstanceName);
            }
        }

        public void Invalidate()
        {
            lock (_lock)
            {
                _processIdsByPoolName.Clear();
                _instanceNameByPoolName.Clear();
            }
        }

        private void RefreshW3WpProcesses()
        {
            Logger.Debug("Refreshing W3WpProcesses");
            foreach (var pair in _instanceNameProvider.GetW3WpProcesses())
            {
                Logger.Debug($"poolName:  {pair.Item1} processid: {pair.Item2}");
                if (_processIdsByPoolName.ContainsKey(pair.Item1))
                    _processIdsByPoolName[pair.Item1] = pair.Item2;
                else
                    _processIdsByPoolName.AddOrUpdate(pair.Item1, pair.Item2, (k,v) => pair.Item2);

                _lastProcessListRefresh = DateTime.UtcNow;
            }
        }
    }
}