using System.Net.Mail;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Crm.Domain;

namespace Plannyt.Api.Modules.Crm.Application;

public static class ClientRequestValidator
{
    public static void Validate(CreateClientRequest request)
    {
        var errors = ValidateCommon(
            request.ClientType,
            request.DisplayName,
            request.CompanyName,
            request.Source,
            request.Person);
        ThrowIfInvalid(errors);
    }

    public static void Validate(
        ClientType clientType,
        UpdateClientRequest request)
    {
        var errors = ValidateCommon(
            clientType,
            request.DisplayName,
            request.CompanyName,
            request.Source,
            request.Person);
        ThrowIfInvalid(errors);
    }

    public static void Validate(UpsertClientContactRequest request)
    {
        var errors = ValidatePerson(new PersonProfileRequest(
            request.FirstName,
            request.LastName,
            request.ContactEmail,
            request.ContactPhone,
            request.PreferredLanguage,
            request.TimeZone));
        if (string.IsNullOrWhiteSpace(request.ContactRole)
            || request.ContactRole.Trim().Length > 80)
        {
            errors["contactRole"] =
                ["El rol del contacto es obligatorio y admite hasta 80 caracteres."];
        }

        ThrowIfInvalid(errors);
    }

    private static Dictionary<string, string[]> ValidateCommon(
        ClientType clientType,
        string displayName,
        string? companyName,
        string? source,
        PersonProfileRequest? person)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 200)
        {
            errors["displayName"] =
                ["El nombre visible es obligatorio y admite hasta 200 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(source) && source.Trim().Length > 100)
        {
            errors["source"] = ["La fuente admite hasta 100 caracteres."];
        }

        if (clientType == ClientType.Person)
        {
            if (person is null)
            {
                errors["person"] = ["El cliente persona requiere un perfil."];
            }
            else
            {
                Merge(errors, ValidatePerson(person), "person.");
            }

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                errors["companyName"] =
                    ["Un cliente persona no puede tener nombre de empresa."];
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(companyName)
                || companyName.Trim().Length > 200)
            {
                errors["companyName"] =
                    ["El nombre de empresa es obligatorio y admite hasta 200 caracteres."];
            }

            if (person is not null)
            {
                errors["person"] =
                    ["El perfil principal solo aplica a clientes persona."];
            }
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidatePerson(
        PersonProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName)
            || request.FirstName.Trim().Length > 100)
        {
            errors["firstName"] =
                ["El nombre es obligatorio y admite hasta 100 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(request.LastName)
            || request.LastName.Trim().Length > 100)
        {
            errors["lastName"] =
                ["El apellido es obligatorio y admite hasta 100 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(request.ContactEmail)
            && (!MailAddress.TryCreate(request.ContactEmail.Trim(), out _)
                || request.ContactEmail.Trim().Length > 254))
        {
            errors["contactEmail"] = ["El correo de contacto no es válido."];
        }

        if (!string.IsNullOrWhiteSpace(request.ContactPhone)
            && request.ContactPhone.Trim().Length > 40)
        {
            errors["contactPhone"] = ["El teléfono admite hasta 40 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(request.PreferredLanguage)
            || request.PreferredLanguage.Trim().Length > 10)
        {
            errors["preferredLanguage"] =
                ["El idioma preferido es obligatorio y admite hasta 10 caracteres."];
        }

        if (!IsValidTimeZone(request.TimeZone))
        {
            errors["timeZone"] = ["La zona horaria IANA no es válida."];
        }

        return errors;
    }

    private static void Merge(
        Dictionary<string, string[]> target,
        IReadOnlyDictionary<string, string[]> source,
        string prefix)
    {
        foreach (var (key, value) in source)
        {
            target[prefix + key] = value;
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
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
