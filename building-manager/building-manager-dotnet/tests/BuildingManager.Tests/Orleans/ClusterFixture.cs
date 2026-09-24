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

    public Task InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Cluster.StopAllSilos();
        return Task.CompletedTask;
    }

    private sealed class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorage("buildingStorage");
        }
    }
}
