using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Plannyt.Api.Modules.Proposals.Security;

public sealed class ProposalTokenService
{
    public ProposalToken Create()
    {
        var value = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));
        return new ProposalToken(value, Hash(value));
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

public sealed record ProposalToken(string Value, string Hash);
