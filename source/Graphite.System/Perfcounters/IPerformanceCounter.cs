using System;

namespace Graphite.System.Perfcounters
{
    public interface IPerformanceCounter:IDisposable
    {
        event EventHandler Disposed;
        string CategoryName { get;  }
        string CounterName { get; }
        string InstanceName { get; }
        float NextValue();
    }
}