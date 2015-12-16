using System;
using System.Collections.Generic;
using System.Linq;
using Graphite.System.Perfcounters;

namespace Graphite.System.Test
{
    public class TestableCounterInstanceNameProvider : ICounterInstanceNameProvider
    {
        public Dictionary<string, string> InstanceNamesByAppPool = new Dictionary<string, string>();
        public Dictionary<string, int> ProcessIdsByAppPool = new Dictionary<string, int>();
        private int _pidSeed = -1000;
        public int WmiQueriesCount = 0;
        public int PerfcounterProcessScanCount = 0;
        private Action<int> _onInstanceGetAction;

        public PoolInstanceMapping Register(string poolName, string instanceName, int? pid=null)
        {
            var pidToUse = pid ?? --_pidSeed;

            if (ProcessIdsByAppPool.ContainsKey(poolName))
                ProcessIdsByAppPool[poolName] = pidToUse;
            else
                ProcessIdsByAppPool.Add(poolName, pidToUse);


            if (InstanceNamesByAppPool.ContainsKey(poolName))
            {
                InstanceNamesByAppPool[poolName] = instanceName;
            }
            else
                InstanceNamesByAppPool.Add(poolName, instanceName);

            return new PoolInstanceMapping() {PoolName = poolName, InstanceName = instanceName};
        }

        public IEnumerable<Tuple<string, int>> GetW3WpProcesses()
        {
            WmiQueriesCount += 1;
            foreach (var pair in ProcessIdsByAppPool)
            {
                yield return Tuple.Create(pair.Key, pair.Value);
            }
        }

        public string GetInstanceName(int processId)
        {
            _onInstanceGetAction?.Invoke(processId);

            PerfcounterProcessScanCount += 1;
            var poolName = ProcessIdsByAppPool.SingleOrDefault(v => v.Value == processId).Key;
            return InstanceNamesByAppPool[poolName];
        }

        public void OnGetInstance(Action<int> action)
        {
            _onInstanceGetAction = action;
        }
    }

    public class PoolInstanceMapping
    {
        public string PoolName;
        public string InstanceName;
    }
}