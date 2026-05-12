using Microsoft.EntityFrameworkCore;
using ProductService.Api.HealthChecks;
using ProductService.Api.Idempotency;
using ProductService.Api.Middleware;
using ProductService.Application;
using ProductService.Infrastructure;
using ProductService.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: read sinks/levels from appsettings; falls back to console.
builder.Host.UseSerilog((ctx, _, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ProductService"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Idempotency cache for the /commit endpoint. Singleton MemoryCache.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("products-db", tags: new[] { "ready" });

// CORS for the Vite dev server (frontend).
const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(
            "http://localhost:5173",   // Vite dev default
            "http://localhost:4173")   // Vite preview
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Auto-migrate + backfill in Development. In production, run migrations from CI/CD.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    await db.Database.MigrateAsync();
    await InventoryBackfillSeeder.RunAsync(db, logger);
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(FrontendCors);
app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
