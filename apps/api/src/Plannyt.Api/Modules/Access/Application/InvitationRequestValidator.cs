using System.Net.Mail;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Access.Application;

public static class InvitationRequestValidator
{
    public static void ValidateTargetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Trim().Length > 254
            || !MailAddress.TryCreate(email.Trim(), out _))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["targetEmail"] = ["El correo objetivo no es válido."]
                });
        }
    }

    public static void Validate(RegisterAndAcceptInvitationRequest request)
    {
        var errors = ValidateProfile(
            request.FirstName,
            request.LastName,
            request.ContactPhone,
            request.PreferredLanguage,
            request.TimeZone);
        if (string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length is < 12 or > 128)
        {
            errors["password"] =
                ["La contraseña debe tener entre 12 y 128 caracteres."];
        }

        ThrowIfInvalid(errors);
    }

    public static void ValidateRequiredProfile(AcceptInvitationRequest request)
    {
        var errors = ValidateProfile(
            request.FirstName,
            request.LastName,
            request.ContactPhone,
            request.PreferredLanguage,
            request.TimeZone);
        ThrowIfInvalid(errors);
    }

    private static Dictionary<string, string[]> ValidateProfile(
        string? firstName,
        string? lastName,
        string? contactPhone,
        string? preferredLanguage,
        string? timeZone)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(firstName)
            || firstName.Trim().Length > 100)
        {
            errors["firstName"] =
                ["El nombre es obligatorio y admite hasta 100 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(lastName)
            || lastName.Trim().Length > 100)
        {
            errors["lastName"] =
                ["El apellido es obligatorio y admite hasta 100 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(contactPhone)
            && contactPhone.Trim().Length > 40)
        {
            errors["contactPhone"] = ["El teléfono admite hasta 40 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(preferredLanguage)
            || preferredLanguage.Trim().Length > 10)
        {
            errors["preferredLanguage"] =
                ["El idioma preferido es obligatorio y admite hasta 10 caracteres."];
        }

        if (!IsValidTimeZone(timeZone))
        {
            errors["timeZone"] = ["La zona horaria IANA no es válida."];
        }

        return errors;
    }

    private static bool IsValidTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
