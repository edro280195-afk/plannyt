using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Organizations.Application;

public static class OrganizationRequestValidator
{
    public static void Validate(UpdateOrganizationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
        {
            errors["name"] = ["El nombre es obligatorio y admite hasta 160 caracteres."];
        }

        if (!IsValidTimeZone(request.TimeZone))
        {
            errors["timeZone"] = ["La zona horaria IANA no es válida."];
        }

        if (request.CountryCode?.Trim().Length != 2)
        {
            errors["countryCode"] = ["El país debe indicarse con un código de dos letras."];
        }

        if (request.CurrencyCode?.Trim().Length != 3)
        {
            errors["currencyCode"] = ["La moneda debe indicarse con un código de tres letras."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
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
}
