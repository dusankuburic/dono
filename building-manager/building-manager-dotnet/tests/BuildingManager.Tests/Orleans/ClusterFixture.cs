using Orleans.TestingHost;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public sealed class ClusterFixture : IAsyncLifetime
{
    public TestCluster Cluster { get; private set; } = null!;

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

                .AddReminders()
                .UseInMemoryReminderService();
        }
    }
}
