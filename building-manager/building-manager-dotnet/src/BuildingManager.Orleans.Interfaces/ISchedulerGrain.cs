using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// Background jobs singleton (key "global"): publishes scheduled announcements
/// as they come due, sends assembly reminders (2 days, 1 day, day-of) and the
/// managers' daily digest of overdue keys and emergency work orders past the
/// 48-hour deadline. Driven by an Orleans reminder every few minutes — which
/// survives silo restarts — and callable directly for a pass on demand.
/// </summary>
public interface ISchedulerGrain : IGrainWithStringKey
{
    /// <summary>Runs one pass of every background job now.</summary>
    Task RunSweepAsync();
}
