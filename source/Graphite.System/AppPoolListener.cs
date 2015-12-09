using System;
using Graphite.System.Perfcounters;

namespace Graphite.System
{
    public class AppPoolListener
    {
        private readonly string appPoolName;
        private readonly string category;
        private readonly string counter;
        private readonly ICounterNameProvider _counterNameProvider;
        private readonly PerformanceCounterFactory _counterFactory;

        private string counterName;

        private CounterListener counterListener;

        public AppPoolListener(string appPoolName, string category, string counter, ICounterNameProvider counterNameProvider, PerformanceCounterFactory counterFactory)
        {
            this.appPoolName = appPoolName;
            this.category = category;
            this.counter = counter;
            _counterNameProvider = counterNameProvider;
            _counterFactory = counterFactory;

            this.LoadCounterName();
        }
        
        public bool LoadCounterName()
        {
            string newName = _counterNameProvider.GetCounterName(this.appPoolName);

            if (!string.IsNullOrEmpty(newName) && this.counterName != newName)
            {
                if (this.counterListener != null)
                {
                    this.counterListener.Dispose();

                    this.counterListener = null;
                }

				this.counterName = newName;
				
                return true;
            }

	        return false;
        }

        public float? ReportValue()
        {
            // AppPool not found -> is not started.
            if (string.IsNullOrEmpty(this.counterName) && !LoadCounterName())
                return null;

            if (this.counterListener == null)
            {
                try
                {
                    this.counterListener = new CounterListener(category, this.counterName, counter, _counterFactory);
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
                this.counterListener = null;

                return null;
            }
        }

     

       
    }
}
