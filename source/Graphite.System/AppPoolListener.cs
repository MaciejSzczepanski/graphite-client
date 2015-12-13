using System;
using Graphite.System.Perfcounters;
using NLog;

namespace Graphite.System
{
    public class AppPoolListener: IDisposable
    {
        private readonly string appPoolName;
        private readonly string category;
        private readonly string counter;
        private readonly CounterInstanceNameCache _counterInstanceNameCache;
        private readonly PerformanceCounterFactory _counterFactory;

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private string counterInstanceName;

        private CounterListener counterListener;

        public AppPoolListener(string appPoolName, string category, string counter, CounterInstanceNameCache counterInstanceNameCache, PerformanceCounterFactory counterFactory)
        {
            this.appPoolName = appPoolName;
            this.category = category;
            this.counter = counter;
            _counterInstanceNameCache = counterInstanceNameCache;
            _counterFactory = counterFactory;

            this.LoadCounterInstanceName();
        }

        public string CategoryName { get { return category; } }

        public string AppPoolName
        {
            get { return appPoolName; }
        }

        public string Counter
        {
            get { return counter; }
        }

        public bool LoadCounterInstanceName()
        {
            string newName = _counterInstanceNameCache.GetCounterInstanceName(this.appPoolName);

            if (!string.IsNullOrEmpty(newName) && this.counterInstanceName != newName)
            {
                if (this.counterListener != null)
                {
                    this.counterListener.Dispose();

                    this.counterListener = null;
                }

				this.counterInstanceName = newName;
				
                return true;
            }

	        return false;
        }

        public float? ReportValue()
        {
            // AppPool not found -> is not started.
            if (string.IsNullOrEmpty(this.counterInstanceName) && !LoadCounterInstanceName())
                return null;

            if (this.counterListener == null)
            {
                try
                {
                    this.counterListener = new CounterListener(category, this.counterInstanceName, counter, _counterFactory);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Warn(ex, "Failed to initialize counter: {0}, {1}, {2}", category,counterInstanceName,counter);
                }
            }

            if (this.counterListener == null)
                return null;

            try
            {
                return this.counterListener.ReportValue(); ;
            }
            catch (InvalidOperationException ex)
            {

                Logger.Warn(ex, "Failed to ReportValue from counter: {0}, {1}, {2}", category, counter, counterInstanceName);
                // counter not available.
                _counterInstanceNameCache.ReportInvalid(appPoolName, counterInstanceName);
                this.counterListener = null;
                this.counterInstanceName = null;

                return null;
            }
        }


        public void Dispose()
        {
            if(counterListener != null)
                counterListener.Dispose();
        }
    }
}
