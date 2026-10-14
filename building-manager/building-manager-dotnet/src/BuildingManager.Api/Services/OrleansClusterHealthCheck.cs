using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orleans;

namespace BuildingManager.Api.Services;

public class OrleansClusterHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private readonly IGrainFactory _grainFactory;

    public OrleansClusterHealthCheck(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _grainFactory.GetGrain<IBuildingRegistryGrain>("global").GetAllAsync()
                .WaitAsync(Timeout, cancellationToken);
            return HealthCheckResult.Healthy("Orleans cluster reachable");
        }
        catch (TimeoutException)
        {
            return HealthCheckResult.Unhealthy("Orleans cluster did not answer within 3 seconds");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Orleans cluster unreachable", ex);
        }
    }
}
