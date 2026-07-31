namespace Plannyt.Api.BuildingBlocks.Http;

public static class LoopbackOrigin
{
    public static bool TryParse(string? origin, out Uri? uri)
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.IsLoopback)
        {
            return true;
        }

        uri = null;
        return false;
    }

    public static bool IsLoopback(string? origin) => TryParse(origin, out _);
}
