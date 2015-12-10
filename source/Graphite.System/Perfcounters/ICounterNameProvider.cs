namespace Graphite.System.Perfcounters
{
    public interface ICounterNameProvider
    {
        string GetCounterName(string poolName);
        void ReportInvalid(string appPoolName, string instanceName);
    }
}