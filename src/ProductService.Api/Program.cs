using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using ProductService.Api.Auth;
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

// ── JWT auth ───────────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtOpts.Secret)
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 32))) // placeholder so DI doesn't crash at startup; will reject any token if real secret is missing
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ProductService API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste token here. Do NOT type Bearer manually."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Idempotency cache for the /commit endpoint. Singleton MemoryCache.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("products-db", tags: new[] { "ready" });

// CORS: permissive in dev and for the public storefront/admin UI.
// In a real production system you'd restrict this to specific frontend
// origins. Wide-open is fine here because the destructive endpoints are
// JWT-guarded via [Authorize(Roles = "Admin")].
const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

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

// AuthN must come BEFORE AuthZ which must come BEFORE MapControllers.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
