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
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var credentials = new SigningCredentials(CreateSigningKey(_options.SigningKey), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse(accessToken, expiresAt, new AuthUserResponse(user.Id, user.Email ?? string.Empty));
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey)
    {
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
