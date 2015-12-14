using System;
using System.Diagnostics;
using Graphite.System.Perfcounters;
using NLog;

namespace Graphite.System
{
    internal class CounterListener : IDisposable
    {
        private readonly string _category;
        private readonly string _instance;
        private readonly string _counterName;
        private readonly PerformanceCounterFactory _counterFactory;
        private IPerformanceCounter _counter;
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();


        public CounterListener(string category, string instance, string counterName,
            PerformanceCounterFactory counterFactory)
        {
            _category = category;
            _instance = instance;
            _counterName = counterName;
            _counterFactory = counterFactory;
            _counter = CreateCounter(category, instance, counterName);
        }

        private IPerformanceCounter CreateCounter(string category, string instance, string counterName)
        {
            IPerformanceCounter counter = null;
            try
            {
                counter = _counterFactory.Create(category, counterName, instance);

                // First call to NextValue returns always 0 -> perforn it without taking value.
                counter.NextValue();
            }
            catch (InvalidOperationException ex)
            {
                string msg = string.Format("Failed to create counter: {0}, {1}, {2}", counter?.CategoryName,
                    counter?.CounterName,
                    counter?.InstanceName);

                Logger.Error(ex, msg);
                throw new InvalidOperationException(msg, ex);
            }

            return counter;
        }

        /// <summary>
        /// Reads the next value from the performance counter.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">The object or underlying performance counter is already disposed.</exception>
        /// <exception cref="InvalidOperationException">Connection to the underlying counter was closed.</exception>
        public float? ReportValue()
        {
            if (_counter == null)
                _counter = CreateCounter(_category, _instance, _counterName);

            try
            {
                // Report current value.
                return this._counter.NextValue();
            }
            catch (InvalidOperationException ex)
            {
                // Connection to the underlying counter was closed.
                Logger.Warn(ex, "Failed to ReportValue from counter: {0}, {1}, {2}", this._counter.CategoryName,
                    this._counter.CounterName, this._counter.InstanceName);
                this._counter.Dispose();
                this._counter = null;

                throw;
            }
        }

        public void Dispose()
        {
            this._counter?.Dispose();
        }
    }
}