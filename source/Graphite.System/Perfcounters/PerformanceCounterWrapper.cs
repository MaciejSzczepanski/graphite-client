using System;
using System.Diagnostics;

namespace Graphite.System.Perfcounters
{
    public class PerformanceCounterWrapper : IPerformanceCounter
    {
        private readonly PerformanceCounter _counter;

        public PerformanceCounterWrapper(PerformanceCounter counter)
        {
            _counter = counter;
        }

        public event EventHandler Disposed
        {
            add { _counter.Disposed += value; }
            remove { _counter.Disposed -= value; }
        }

        public float NextValue()
        {
            return _counter.NextValue();
        }

        public void Dispose()
        {
            _counter.Dispose();
        }

        public string CategoryName
        {
            get { return _counter.CategoryName; }
            set { _counter.CategoryName = value; }
        }

        public string CounterName
        {
            get { return _counter.CounterName; }
            set { _counter.CounterName = value; }
        }

        public string InstanceName
        {
            get { return _counter.InstanceName; }
            set { _counter.InstanceName = value; }
        }
    }
}