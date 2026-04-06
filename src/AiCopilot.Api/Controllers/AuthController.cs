using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiCopilot.Application.Abstractions;
using AiCopilot.Api.Options;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly JwtOptions _jwtOptions;
    private readonly IReadOnlyList<AuthUserOptions> _users;

    public AuthController(
        IAuditLogService auditLogService,
        IOptions<JwtOptions> jwtOptions,
        IReadOnlyList<AuthUserOptions> users)
    {
        _auditLogService = auditLogService;
        _jwtOptions = jwtOptions.Value;
        _users = users;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Token([FromBody] AuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var user = _users.FirstOrDefault(x =>
            string.Equals(x.Username, request.Username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            x.Password == request.Password);

        if (user is null)
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var expiresUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("tenant_id", user.TenantId)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        await _auditLogService.WriteAsync(
            action: "TOKEN_ISSUED",
            metadata: $$"""
            {"username":"{{user.Username}}","role":"{{user.Role}}","tenantId":"{{user.TenantId}}"}
            """,
            cancellationToken: cancellationToken);

        return Ok(new AuthTokenResponse(accessToken, expiresUtc, user.Role, user.TenantId));
    }
}
