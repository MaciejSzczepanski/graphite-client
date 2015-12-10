using System;
using System.Collections.Generic;

namespace Graphite.System.Perfcounters
{
    public interface ICounterInstanceNameProvider
    {
        IEnumerable<Tuple<string, int>> GetW3WpProcesses();
        string GetInstanceName(int processId);
    }
}