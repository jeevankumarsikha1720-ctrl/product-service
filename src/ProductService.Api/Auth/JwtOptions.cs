namespace ProductService.Api.Auth;

/// <summary>
/// Strongly-typed JWT settings. Bound from configuration ("Auth" section).
/// In production the Secret + admin password come from App Service environment
/// variables, NOT appsettings.json. In dev, set via dotnet user-secrets:
///   dotnet user-secrets set "Auth:Secret" "..."
///   dotnet user-secrets set "Auth:AdminPassword" "..."
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth";

    /// <summary>Signing key for HS256. Must be at least 32 characters / 256 bits.</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ProductService";
    public string Audience { get; set; } = "ProductServiceUI";

    /// <summary>Token lifetime in minutes. Default 8 hours - covers a typical work session.</summary>
    public int TokenExpiryMinutes { get; set; } = 480;

    /// <summary>Single hardcoded admin user. For a single-admin portfolio system that's enough.</summary>
    public string AdminUsername { get; set; } = "admin";

    /// <summary>Admin password. Compared as plaintext - set via env var in production.</summary>
    public string AdminPassword { get; set; } = string.Empty;
}
