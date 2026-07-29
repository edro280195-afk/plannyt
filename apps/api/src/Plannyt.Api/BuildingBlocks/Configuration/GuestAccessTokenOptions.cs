using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class GuestAccessTokenOptions
{
    public const string SectionName = "GuestAccessTokens";

    [Required]
    [MinLength(1)]
    public string ActiveKeyId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public Dictionary<string, string> Keys { get; init; } = [];

    public bool IsConfigured => Keys.Count > 0 && !string.IsNullOrEmpty(ActiveKeyId);

    public string GetDerivationKey(string keyId)
    {
        if (!Keys.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException(
                $"La llave de derivación '{keyId}' no existe.");
        }

        if (key.Length < 64)
        {
            throw new InvalidOperationException(
                $"La llave de derivación '{keyId}' debe tener al menos 64 caracteres.");
        }

        return key;
    }

    public string GetActiveDerivationKey() => GetDerivationKey(ActiveKeyId);
}
