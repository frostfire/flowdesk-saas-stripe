using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowDesk.Contracts.Auth;
using FlowDesk.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlowDesk.Api.Auth;

public sealed class JwtTokenService
{
    public const string TokenTypeClaim = "token_type";
    public const string AccessTokenType = "access";
    public const string RefreshTokenType = "refresh";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AuthResponse CreateToken(ApplicationUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(TokenTypeClaim, AccessTokenType),
        };

        var accessToken = WriteToken(claims, expiresAt);
        return new AuthResponse(accessToken, expiresAt, new AuthUserResponse(user.Id, user.Email ?? string.Empty));
    }

    public string CreateRefreshToken(ApplicationUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(TokenTypeClaim, RefreshTokenType),
        };

        return WriteToken(claims, expiresAt);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, CreateValidationParameters(), out _);
        return principal.FindFirstValue(TokenTypeClaim) == RefreshTokenType ? principal : null;
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey)
    {
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    public TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(_options.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTimeOffset expiresAt)
    {
        var credentials = new SigningCredentials(CreateSigningKey(_options.SigningKey), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
