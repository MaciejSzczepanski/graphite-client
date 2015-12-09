namespace Graphite.System
{
    public interface ICounterNameProvider
    {
        string GetCounterName(string appPool);
    }
}