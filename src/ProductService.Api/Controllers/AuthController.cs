using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProductService.Api.Auth;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IJwtTokenService tokenService,
    IOptions<JwtOptions> options) : ControllerBase
{
    private readonly JwtOptions _opts = options.Value;

    public sealed record LoginRequest(string Username, string Password);
    public sealed record LoginResponse(string Token, string Username, string[] Roles, DateTime ExpiresAtUtc);

    /// <summary>
    /// Validates credentials against the configured admin user and returns a JWT.
    /// Rate limiting is not implemented here - a public-internet deployment should
    /// add rate limiting middleware or fronting via API Management.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { error = "Username and password are required." });
        }

        var validUser = string.Equals(request.Username, _opts.AdminUsername, StringComparison.Ordinal);
        var validPass = string.Equals(request.Password, _opts.AdminPassword, StringComparison.Ordinal);

        if (!validUser || !validPass)
        {
            // Same response for "no such user" and "wrong password" - don't leak which.
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var roles = new[] { "Admin" };
        var token = tokenService.Issue(request.Username, roles);
        var expiresAt = DateTime.UtcNow.AddMinutes(_opts.TokenExpiryMinutes);

        return Ok(new LoginResponse(token, request.Username, roles, expiresAt));
    }
}
