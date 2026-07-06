using DailyVitals.Data.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DailyVitals.Web.Health;

internal sealed class DatabaseReadinessHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await DbConnectionFactory.ValidateRuntimeSecurityAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "The database is unavailable or its connection is insecure.",
                exception);
        }
    }
}
