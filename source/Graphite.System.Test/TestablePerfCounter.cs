using System;
using Graphite.System.Perfcounters;

namespace Graphite.System.Test
{
    public class TestablePerfCounter : IPerformanceCounter
    {
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        private bool _isInvalid;
        public string CategoryName { get; set; }
        public string CounterName { get; set; }
        public string InstanceName { get; set; }

        public TestablePerfCounter(string category, string counter, string instance)
        {
            CategoryName = category;
            CounterName = counter;
            InstanceName = instance;
        }
        
        public event EventHandler Disposed;
        
        public float NextValue()
        {
            if (_isInvalid)
                throw new InvalidOperationException("Counter not available");

            return Convert.ToSingle(_random.NextDouble());
        }

        public void MarkAsInvalid()
        {
            _isInvalid = true;
        }


        public void Dispose()
        {
            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}