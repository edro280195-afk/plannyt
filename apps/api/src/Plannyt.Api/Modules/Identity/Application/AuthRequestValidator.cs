using System.ComponentModel.DataAnnotations;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Identity.Application;

public static class AuthRequestValidator
{
    public static void Validate(RegisterPlannerRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        ValidateEmail(request.Email, errors);
        ValidatePassword(request.Password, errors);
        AddRequired(errors, nameof(request.FirstName), request.FirstName, 100);
        AddRequired(errors, nameof(request.LastName), request.LastName, 100);
        AddRequired(
            errors,
            nameof(request.OrganizationName),
            request.OrganizationName,
            160);

        if (!IsValidTimeZone(request.TimeZone))
        {
            errors[nameof(request.TimeZone)] = ["La zona horaria no es válida."];
        }

        if (request.CountryCode.Trim().Length != 2)
        {
            errors[nameof(request.CountryCode)] =
                ["El código de país debe tener dos caracteres."];
        }

        if (request.CurrencyCode.Trim().Length != 3)
        {
            errors[nameof(request.CurrencyCode)] =
                ["El código de moneda debe tener tres caracteres."];
        }

        ThrowIfInvalid(errors);
    }

    public static void Validate(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        ValidateEmail(request.Email, errors);

        if (string.IsNullOrEmpty(request.Password))
        {
            errors[nameof(request.Password)] = ["La contraseña es obligatoria."];
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateEmail(
        string email,
        IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 254
            || !new EmailAddressAttribute().IsValid(email))
        {
            errors["Email"] = ["El correo electrónico no es válido."];
        }
    }

    private static void ValidatePassword(
        string password,
        IDictionary<string, string[]> errors)
    {
        if (password.Length is < 12 or > 128)
        {
            errors["Password"] =
                ["La contraseña debe tener entre 12 y 128 caracteres."];
        }
    }

    private static void AddRequired(
        IDictionary<string, string[]> errors,
        string field,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["El campo es obligatorio."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"El campo admite máximo {maxLength} caracteres."];
        }
    }

    private static bool IsValidTimeZone(string timeZone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
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

    private static void ThrowIfInvalid(
        IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
