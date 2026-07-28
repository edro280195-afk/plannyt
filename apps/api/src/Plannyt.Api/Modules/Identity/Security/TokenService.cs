using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.Modules.Identity.Domain;

namespace Plannyt.Api.Modules.Identity.Security;

public sealed class TokenService(
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public AccessTokenResult CreateAccessToken(
        UserAccount account,
        UserSession session)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sid", session.Id.ToString()),
            new Claim(
                "security_version",
                account.SecurityVersion.ToString(CultureInfo.InvariantCulture))
        };
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var expiresAt = timeProvider.GetUtcNow().AddDays(_options.RefreshTokenDays);
        return new RefreshTokenResult(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenResult(
    string Token,
    string TokenHash,
    DateTimeOffset ExpiresAt);
