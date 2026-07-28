namespace Plannyt.Api.BuildingBlocks.Errors;

public abstract class ApiException(
    int statusCode,
    string title,
    string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;
}

public sealed class NotFoundException(string detail)
    : ApiException(StatusCodes.Status404NotFound, "Recurso no encontrado", detail);

public sealed class ForbiddenException(string detail)
    : ApiException(StatusCodes.Status403Forbidden, "Acceso denegado", detail);

public sealed class ConflictException(string detail)
    : ApiException(StatusCodes.Status409Conflict, "Conflicto", detail);

public sealed class GoneException(string detail)
    : ApiException(StatusCodes.Status410Gone, "El recurso ya no está disponible", detail);

public sealed class PayloadTooLargeException(string detail)
    : ApiException(
        StatusCodes.Status413PayloadTooLarge,
        "El contenido excede el límite permitido",
        detail);

public sealed class UnsupportedMediaTypeException(string detail)
    : ApiException(
        StatusCodes.Status415UnsupportedMediaType,
        "Tipo de archivo no permitido",
        detail);

public sealed class UnauthorizedException(string detail)
    : ApiException(StatusCodes.Status401Unauthorized, "No autorizado", detail);

public sealed class PublicInvitationUnavailableException(
    int statusCode,
    string reason,
    string detail)
    : ApiException(statusCode, "Invitación no disponible", detail)
{
    public string Reason { get; } = reason;
}

public sealed class RequestValidationException(
    IReadOnlyDictionary<string, string[]> errors)
    : ApiException(
        StatusCodes.Status400BadRequest,
        "La solicitud no es válida",
        "Revisa los campos indicados.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
