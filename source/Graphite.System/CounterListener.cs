﻿using System;
using System.Diagnostics;
using Graphite.System.Perfcounters;

namespace Graphite.System
{
    internal class CounterListener : IDisposable
    {
        private readonly PerformanceCounterFactory _counterFactory;
        private IPerformanceCounter counter;
        
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
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    exception.Message + string.Format(" (Category: '{0}', Counter: '{1}', Instance: '{2}')", category, counter, instance),
                    exception);
            }
        }

        /// <summary>
        /// Reads the next value from the performance counter.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ObjectDisposedException">The object or underlying performance counter is already disposed.</exception>
        /// <exception cref="System.InvalidOperationException">Connection to the underlying counter was closed.</exception>
        public float? ReportValue()
        {
            if (this.disposed)
                throw new ObjectDisposedException(typeof(PerformanceCounter).Name);

            try
            {
                // Report current value.
                return this.counter.NextValue();
            }
            catch (InvalidOperationException)
            {
                // Connection to the underlying counter was closed.

                this.Dispose(true);

                //this.RenewCounter();

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
