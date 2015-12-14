using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Graphite.Configuration;
using Graphite.Infrastructure;
using Graphite.System.Configuration;
using Graphite.System.Perfcounters;
using NLog;

namespace Graphite.System
{
    internal class Kernel : IDisposable
    {
        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const short RetryInterval = 60;

        private readonly Scheduler scheduler;

        private readonly ChannelFactory factory;

        private readonly List<CounterListener> counters = new List<CounterListener>();

        private readonly List<EventlogListener> listeners = new List<EventlogListener>();

        private readonly List<AppPoolListener> appPools = new List<AppPoolListener>();

        private readonly List<CounterListenerElement> retryCreation = new List<CounterListenerElement>();

        private bool disposed;

        private readonly CounterInstanceNameCache _counterInstanceNameCache;
        private readonly PerformanceCounterFactory _counterFactory = new PerformanceCounterFactory();
        private int _appPoolListenerRefreshInterval;

        public Kernel(IConfigurationContainer configuration, GraphiteSystemConfiguration systemConfiguration)
        {
            _counterInstanceNameCache = new CounterInstanceNameCache(new WmiCounterInstanceNameProvider())
            {
                Expiration = TimeSpan.FromSeconds(systemConfiguration.SystemSettings.InstanceNameCacheExpiration)
            };

            _appPoolListenerRefreshInterval = systemConfiguration.SystemSettings.AppPoolListenerRefreshInterval;

            this.factory = new ChannelFactory(configuration.Graphite, configuration.StatsD);

            foreach (var listener in systemConfiguration.EventlogListeners.Cast<EventlogListenerElement>())
            {
                this.CreateEventlogListener(listener);
            }

            this.scheduler = new Scheduler();

            foreach (var listener in systemConfiguration.CounterListeners.Cast<CounterListenerElement>())
            {
                Action action;

                try
                {
                    action = this.CreateReportingAction(listener);

                    this.scheduler.Add(action, listener.Interval);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error(ex,"Failed to create CounterListener for: {0} {1} {2}, interval {3}", listener.Category, listener.Counter, listener.Instance, listener.Interval);

                    if (!listener.Retry)
                        throw;

                    this.retryCreation.Add(listener);
                }
            }

            if (this.retryCreation.Any())
            {
                this.scheduler.Add(this.RetryCounterCreation, RetryInterval);
            }

            foreach (var appPool in systemConfiguration.AppPool.Cast<AppPoolElement>())
            {
                try
                {
                    AppPoolListener element;

                    var action = this.CreateReportingAction(appPool, out element);

                    this.scheduler.Add(action, appPool.Interval);

                    this.scheduler.Add(() => element.LoadCounterInstanceName(), _appPoolListenerRefreshInterval);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Failed to create appPoolListerner for: {0}, counter: {1} {2}", appPool.AppPoolName, appPool.Category, appPool.Counter), ex);
                }
             }
            
            this.scheduler.Start();
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
                if (this.scheduler != null)
                {
                    this.scheduler.Dispose();
                }

                if (this.factory != null)
                {
                    this.factory.Dispose();
                }

                foreach (CounterListener listener in this.counters)
                {
                    listener.Dispose();
                }

                foreach (EventlogListener listener in this.listeners)
                {
                    listener.Dispose();
                }

                foreach (var listener in this.appPools)
                {
                    listener.Dispose();
                }

                this.disposed = true;
            }
        }

        private Action CreateReportingAction(CounterListenerElement config)
        {
            CounterListener listener = new CounterListener(config.Category, config.Instance, config.Counter, _counterFactory);
            
            IMonitoringChannel channel;

            if (config.Sampling.HasValue)
            {
                channel = this.factory.CreateChannel(config.Type, config.Target, config.Sampling.Value);
            }
            else
            {
                channel = this.factory.CreateChannel(config.Type, config.Target);
            }

            this.counters.Add(listener);

            return () =>
            {
                float? value = null;
                try
                {
                    value = listener.ReportValue();
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error(ex,"Failed to report value from counter. It will retry on next interval" );
                }

                if (value.HasValue)
                {
                    channel.Report(config.Key, value.Value);
                }
            };
        }

        private Action CreateReportingAction(AppPoolElement config, out AppPoolListener listener)
        {

            AppPoolListener element = null;

            if (config.WorkingSet && string.IsNullOrEmpty(config.Counter))
            {
                element = new AppPoolListener(config.AppPoolName, "Process", "Working Set", _counterInstanceNameCache, _counterFactory);
            } 
            else if (!string.IsNullOrEmpty(config.Counter))
            {
                element = new AppPoolListener(config.AppPoolName, config.Category, config.Counter, _counterInstanceNameCache, _counterFactory);
            }

            listener = element;
            
            IMonitoringChannel channel = this.factory.CreateChannel(config.Type, config.Target);

            this.appPools.Add(element);

            return () =>
            {
                if (element != null)
                {
                    float? value = element.ReportValue();

                    if (value.HasValue)
                    {
                        channel.Report(config.Key, value.Value);
                    }
                }
            };
        }

        private void CreateEventlogListener(EventlogListenerElement config)
        {
            IMonitoringChannel channel;

            if (config.Sampling.HasValue)
            {
                channel = this.factory.CreateChannel(config.Type, config.Target, config.Sampling.Value);
            }
            else
            {
                channel = this.factory.CreateChannel(config.Type, config.Target);
            }

            EventLogEntryType[] types = config.EntryTypes
                .Split(new []{ ';', ',' })
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), s.Trim()))
                .ToArray();

            var listener = new EventlogListener(
                config.Protocol,
                config.Source,
                config.Category,
                types,
                config.Key,
                config.Value,
                channel);

            this.listeners.Add(listener);
        }

        private void RetryCounterCreation()
        {
            foreach (CounterListenerElement listener in new List<CounterListenerElement>(this.retryCreation))
            {
                try
                {
                    Action action = this.CreateReportingAction(listener);

                    this.scheduler.Add(action, listener.Interval);
                    
                    this.retryCreation.Remove(listener);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error(ex, "Failed to recreate CounterListener for: {0} {1} {2}, interval {3}", listener.Category, listener.Counter, listener.Instance, listener.Interval);
                }
            }

            if (!this.retryCreation.Any())
            {
                this.scheduler.Remove(this.RetryCounterCreation, RetryInterval);
            }
        }
    }
}
