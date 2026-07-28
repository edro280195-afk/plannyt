using System.Net.Mail;
using System.Text.RegularExpressions;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Guests.Application;

public static partial class GuestRequestValidator
{
    private static readonly IReadOnlySet<string> ColorTokens =
        new HashSet<string>(
            ["slate", "rose", "amber", "emerald", "sky", "indigo", "violet"],
            StringComparer.Ordinal);

    public static void Validate(InvitationGroupRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DisplayName)
            || request.DisplayName.Trim().Length > 160)
        {
            errors["displayName"] = ["El nombre del grupo es obligatorio y admite 160 caracteres."];
        }

        if (request.ContactEmail is not null && !IsValidEmail(request.ContactEmail))
        {
            errors["contactEmail"] = ["El correo de contacto no es válido."];
        }

        if (request.ContactPhone?.Length > 32)
        {
            errors["contactPhone"] = ["El teléfono admite hasta 32 caracteres."];
        }

        ThrowIfAny(errors);
    }

    public static void Validate(EventGuestRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.IsNamed
            && string.IsNullOrWhiteSpace(request.FirstName)
            && string.IsNullOrWhiteSpace(request.LastName))
        {
            errors["firstName"] = ["Un invitado con nombre requiere nombre o apellido."];
        }

        if (request.FirstName.Length > 100 || request.LastName.Length > 100)
        {
            errors["name"] = ["El nombre y apellido admiten hasta 100 caracteres cada uno."];
        }

        if (request.Email is not null && !IsValidEmail(request.Email))
        {
            errors["email"] = ["El correo no es válido."];
        }

        if (request.Phone?.Length > 32)
        {
            errors["phone"] = ["El teléfono admite hasta 32 caracteres."];
        }

        if (request.SortOrder < 0)
        {
            errors["sortOrder"] = ["El orden no puede ser negativo."];
        }

        ThrowIfAny(errors);
    }

    public static void Validate(GuestTagRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 60)
        {
            errors["name"] = ["La etiqueta es obligatoria y admite 60 caracteres."];
        }

        if (!ColorTokens.Contains(request.ColorToken))
        {
            errors["colorToken"] = ["Selecciona un color permitido."];
        }

        ThrowIfAny(errors);
    }

    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = NonDigitRegex().Replace(value, string.Empty);
        return digits.Length < 7 ? value.Trim().ToLowerInvariant() : digits;
    }

    public static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    public static bool IsValidEmail(string value)
    {
        try
        {
            return new MailAddress(value.Trim()).Address == value.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex NonDigitRegex();
}
