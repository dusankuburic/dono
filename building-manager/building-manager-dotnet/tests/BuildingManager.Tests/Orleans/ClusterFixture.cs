using Orleans.TestingHost;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// In-process Orleans test cluster. Uses in-memory storage in place of the
/// AdoNet provider configured in the real silo, so no database is needed.
/// Grain calls still go through the full serialization pipeline, which is what
/// caught the lazy-LINQ-iterator return bug in production.
/// </summary>
public sealed class ClusterFixture : IAsyncLifetime
{
    public TestCluster Cluster { get; private set; } = null!;

    // Async all the way: the blocking Deploy()/StopAllSilos() starve xUnit's
    // limited worker threads once more test classes start clusters at once
    // than there are workers, and the whole run hangs.
    public async Task InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await Cluster.DisposeAsync();
    }

    private sealed class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddMemoryGrainStorage("buildingStorage")
                // The scheduler grain registers a reminder on activation; the
                // in-memory reminder table keeps tests database-free.
                .AddReminders()
                .UseInMemoryReminderService();
        }
    }
}
