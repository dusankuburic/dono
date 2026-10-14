using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface ISchedulerGrain : IGrainWithStringKey
{
    Task EnsureScheduledAsync();

    Task RunSweepAsync();
}
