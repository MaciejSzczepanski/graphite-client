namespace Graphite.System.Perfcounters
{
    public interface ICounterInstanceNameProvider
    {
        string GetCounterInstanceName(string poolName);
        void ReportInvalid(string appPoolName, string instanceName);
    }
}