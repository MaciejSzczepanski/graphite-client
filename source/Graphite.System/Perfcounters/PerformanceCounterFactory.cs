using System.Diagnostics;

namespace Graphite.System.Perfcounters
{
    public class PerformanceCounterFactory
    {
        public virtual IPerformanceCounter Create(string category, string counter, string instance)
        {
            return new PerformanceCounterWrapper(new PerformanceCounter(category,counter,instance,true));
        }
    }
}
