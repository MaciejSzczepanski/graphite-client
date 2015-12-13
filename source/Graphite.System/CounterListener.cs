using System;
using System.Diagnostics;
using Graphite.System.Perfcounters;
using NLog;

namespace Graphite.System
{
    internal class CounterListener : IDisposable
    {
        private readonly PerformanceCounterFactory _counterFactory;
        private IPerformanceCounter counter;
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private bool disposed;

        public CounterListener(string category, string instance, string counter, PerformanceCounterFactory counterFactory)
        {
            _counterFactory = counterFactory;
            try
            {
                this.counter = _counterFactory.Create(category, counter, instance);
                this.counter.Disposed += (sender, e) => this.disposed = true;

                // First call to NextValue returns always 0 -> perforn it without taking value.
                this.counter.NextValue();
            }
            catch (InvalidOperationException ex)
            {

                Logger.Error(ex, "Failed to ReportValue from counter: {0}, {1}, {2}", this.counter.CategoryName, this.counter.CounterName, this.counter.InstanceName);
                throw new InvalidOperationException(
                    ex.Message + string.Format(" (Category: '{0}', Counter: '{1}', Instance: '{2}')", category, counter, instance),
                    ex);
            }
        }

        /// <summary>
        /// Reads the next value from the performance counter.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">The object or underlying performance counter is already disposed.</exception>
        /// <exception cref="InvalidOperationException">Connection to the underlying counter was closed.</exception>
        public float? ReportValue()
        {
            if (this.disposed)
                throw new ObjectDisposedException(typeof(PerformanceCounter).Name);

            try
            {
                // Report current value.
                return this.counter.NextValue();
            }
            catch (InvalidOperationException ex)
            {
                // Connection to the underlying counter was closed.
                Logger.Warn(ex, "Failed to ReportValue from counter: {0}, {1}, {2}", this.counter.CategoryName, this.counter.CounterName, this.counter.InstanceName);
                this.Dispose(true);
            
                throw;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                if (this.counter != null)
                {
                    this.counter.Dispose();
                }

                this.disposed = true;
            }
        }

        protected virtual void RenewCounter()
        {
            this.counter = _counterFactory.Create(this.counter.CategoryName,
                this.counter.CounterName,
                this.counter.InstanceName) ;

            this.counter.Disposed += (sender, e) => this.disposed = true;

            this.disposed = false;

            try
            {
                // First call to NextValue returns always 0 -> perforn it without taking value.
                this.counter.NextValue();
            }
            catch (InvalidOperationException)
            {
                // nop
            }
        }
    }
}
