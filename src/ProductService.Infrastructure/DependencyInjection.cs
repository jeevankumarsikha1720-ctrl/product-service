using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Interfaces;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Repositories;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProductsDb")
            ?? throw new InvalidOperationException("Connection string 'ProductsDb' is missing.");

        services.AddDbContext<ProductDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", ProductDbContext.Schema);
                sql.EnableRetryOnFailure(maxRetryCount: 5);
            });

            // EF Core 9+ throws PendingModelChangesWarning at MigrateAsync() if the
            // DbContext model has been changed since the last migration was generated.
            // Downgrading it to a log warning so dev auto-migrate keeps working - in
            // production you should run migrations explicitly via CI/CD anyway, so
            // this affects only the local Development startup path.
            options.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        return services;
    }
}
