using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;

namespace Plannyt.Api.Modules.Invitations.Security;

public sealed class GuestAccessTokenService(
    IOptions<GuestAccessTokenOptions> options)
{
    private readonly byte[] _derivationKey =
        Encoding.UTF8.GetBytes(options.Value.DerivationKey);

    public GuestAccessToken Create(Guid linkId)
    {
        var value = DeriveValue(linkId);
        return new GuestAccessToken(value, Hash(value));
    }

    public string Reveal(Guid linkId) => DeriveValue(linkId);

    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
        {
            return string.Empty;
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private string DeriveValue(Guid linkId)
    {
        using var hmac = new HMACSHA384(_derivationKey);
        var purpose = Encoding.UTF8.GetBytes(
            $"plannyt:guest-access-link:v1:{linkId:N}");
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(purpose));
    }
}

public sealed record GuestAccessToken(string Value, string Hash);
