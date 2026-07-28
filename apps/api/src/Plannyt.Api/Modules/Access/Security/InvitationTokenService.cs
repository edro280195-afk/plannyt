using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Plannyt.Api.Modules.Access.Security;

public sealed class InvitationTokenService
{
    public InvitationTokenResult Create()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return new InvitationTokenResult(token, Hash(token));
    }

    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
        {
            return string.Empty;
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

public sealed record InvitationTokenResult(string Token, string TokenHash);
