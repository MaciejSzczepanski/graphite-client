using System;
using Graphite.System.Perfcounters;

namespace Graphite.System
{
    public class AppPoolListener
    {
        private readonly string appPoolName;
        private readonly string category;
        private readonly string counter;
        private readonly ICounterInstanceNameProvider _counterInstanceNameProvider;
        private readonly PerformanceCounterFactory _counterFactory;

        private string counterInstanceName;

        private CounterListener counterListener;

        public AppPoolListener(string appPoolName, string category, string counter, ICounterInstanceNameProvider counterInstanceNameProvider, PerformanceCounterFactory counterFactory)
        {
            this.appPoolName = appPoolName;
            this.category = category;
            this.counter = counter;
            _counterInstanceNameProvider = counterInstanceNameProvider;
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
            string newName = _counterInstanceNameProvider.GetCounterInstanceName(this.appPoolName);

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
                catch (InvalidOperationException)
                { 
                }
            }

            if (this.counterListener == null)
                return null;

            try
            {
                return this.counterListener.ReportValue(); ;
            }
            catch (InvalidOperationException)
            {
                // counter not available.
                _counterInstanceNameProvider.ReportInvalid(appPoolName, counterInstanceName);
                this.counterListener = null;
                this.counterInstanceName = null;

                return null;
            }
        }

     

       
    }
}
