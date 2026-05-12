using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Api.HealthChecks;

/// <summary>
/// Readiness probe for PostgreSQL. Uses the existing DbContext rather than
/// a separate connection so we don't pull in vulnerable third-party packages.
/// </summary>
public sealed class DatabaseHealthCheck(ProductDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL probe threw an exception.", ex);
        }
    }
}
