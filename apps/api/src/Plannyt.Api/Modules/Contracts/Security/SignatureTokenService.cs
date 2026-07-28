using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Plannyt.Api.Modules.Contracts.Security;

public sealed class SignatureTokenService
{
    public SignatureToken Create()
    {
        var value = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));
        return new SignatureToken(value, Hash(value));
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

public sealed record SignatureToken(string Value, string Hash);
