using System.Collections.Generic;
using System.Linq;
using Graphite.System.Perfcounters;

namespace Graphite.System.Test
{
    public class TestablePerformanceCounterFactory : PerformanceCounterFactory
    {
        public override IPerformanceCounter Create(string category, string counter, string instance)
        {
            var testableCounter =
                CreatedCounters.SingleOrDefault(
                    c => c.CategoryName == category && c.CounterName == counter && c.InstanceName == instance);

            if (testableCounter == null)
            {
                testableCounter = new TestablePerfCounter(category, counter, instance);
                CreatedCounters.Add(testableCounter);
            }
            return testableCounter;
        }

        public List<TestablePerfCounter>  CreatedCounters = new List<TestablePerfCounter>();
    }
}