using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace Graphite.System
{
    internal class Scheduler : IDisposable
    {
        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, List<Action>> actions = new Dictionary<int, List<Action>>();

        private Timer timer;

        private volatile uint counter;

        private bool disposed;

        public void Start()
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }

            // Initialize timer with interval of 1 second.
            this.timer = new Timer(this.TimerAction, null, 0, 1000);
            this.counter = 0;
        }

        public void Stop()
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval">The invocation interval in seconds.</param>
        /// <returns></returns>
        public Scheduler Add(Action action, int interval)
        {
            if (!this.actions.ContainsKey(interval))
            {
                this.actions.Add(interval, new List<Action>());
            }

            this.actions[interval].Add(action);

            return this;
        }

        public bool Remove(Action action, int interval)
        {
            if (!this.actions.ContainsKey(interval))
                return false;

            return this.actions[interval].Remove(action);
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
                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;
                }

                this.disposed = true;
            }
        }

        private void TimerAction(object state)
        {
            long localCounter = ++this.counter;

            foreach (int interval in this.actions.Keys)
            {
                if (localCounter%interval == 0)
                {
                    if (Logger.IsInfoEnabled)
                        Logger.Info("Starting actions for interval: " + interval);

                    var sw = Stopwatch.StartNew();
                    foreach (var a in this.actions[interval])
                    {
                        try
                        {
                            a.Invoke();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Failed to execute action");
                        }
                    }
                    sw.Stop();

                    if(Logger.IsWarnEnabled)
                        if (sw.Elapsed.TotalSeconds >= 1f){ Logger.Warn("Collecting perfcounters is slow. Duration: {0}ms (count: {1}) for interval: {2}", sw.Elapsed.TotalMilliseconds, actions[interval].Count, interval);  }

                    if (Logger.IsInfoEnabled)
                        Logger.Info("Finished actions. Duration: {0}ms (count: {1}) for interval: {2}", sw.Elapsed.TotalMilliseconds, actions[interval].Count, interval);
                }
            }
        }
    }
}