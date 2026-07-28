namespace Plannyt.Api.Modules.Identity.Application;

public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
