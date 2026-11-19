using Microsoft.Extensions.Diagnostics.HealthChecks;
using Plaza.Groups.Contracts;
using Plaza.Profiles.Contracts;

namespace Plaza.Api.Health;

/// <summary>Ready when the module stores answer. Slice 2 adds the database ping here.</summary>
internal sealed class ModulesReadyCheck(IGroupDirectory groups, IProfileDirectory profiles) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _ = groups.FindGroup("health-probe");
        _ = profiles.Find("health-probe");
        return Task.FromResult(HealthCheckResult.Healthy("Module stores reachable."));
    }
}
