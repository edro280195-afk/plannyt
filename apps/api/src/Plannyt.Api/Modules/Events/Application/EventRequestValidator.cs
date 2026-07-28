using System.Net.Mail;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Events.Application;

public static class EventRequestValidator
{
    public static void Validate(CreateEventRequest request) =>
        ValidateDetails(
            request.Name,
            request.EventType,
            request.StartDateTime,
            request.EndDateTime,
            request.TimeZone,
            request.City,
            request.CountryCode,
            request.SharedDescription,
            request.EstimatedGuestCount);

    public static void Validate(UpdateEventRequest request) =>
        ValidateDetails(
            request.Name,
            request.EventType,
            request.StartDateTime,
            request.EndDateTime,
            request.TimeZone,
            request.City,
            request.CountryCode,
            request.SharedDescription,
            request.EstimatedGuestCount);

    public static void Validate(UpsertEventParticipantRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequiredText(errors, "firstName", request.FirstName, 100, "nombre");
        ValidateRequiredText(errors, "lastName", request.LastName, 100, "apellido");
        ValidateRequiredText(
            errors,
            "preferredLanguage",
            request.PreferredLanguage,
            10,
            "idioma preferido");
        ValidateRequiredText(
            errors,
            "participantType",
            request.ParticipantType,
            80,
            "tipo de participante");

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

        if (!IsValidTimeZone(request.TimeZone))
        {
            errors["timeZone"] = ["La zona horaria IANA no es válida."];
        }

        if (request.DisplayOrder < 0)
        {
            errors["displayOrder"] = ["El orden no puede ser negativo."];
        }

        if (!string.IsNullOrWhiteSpace(request.SharedDescription)
            && request.SharedDescription.Trim().Length > 1000)
        {
            errors["sharedDescription"] =
                ["La descripción compartida admite hasta 1000 caracteres."];
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateDetails(
        string name,
        string eventType,
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime,
        string timeZone,
        string city,
        string countryCode,
        string? sharedDescription,
        int? estimatedGuestCount)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequiredText(errors, "name", name, 200, "nombre");
        ValidateRequiredText(errors, "eventType", eventType, 80, "tipo de evento");
        ValidateRequiredText(errors, "city", city, 120, "ciudad");

        if (endDateTime < startDateTime)
        {
            errors["endDateTime"] =
                ["La fecha de fin no puede ser anterior a la fecha de inicio."];
        }

        if (!IsValidTimeZone(timeZone))
        {
            errors["timeZone"] = ["La zona horaria IANA no es válida."];
        }

        if (countryCode?.Trim().Length != 2)
        {
            errors["countryCode"] =
                ["El país debe indicarse con un código de dos letras."];
        }

        if (!string.IsNullOrWhiteSpace(sharedDescription)
            && sharedDescription.Trim().Length > 2000)
        {
            errors["sharedDescription"] =
                ["La descripción compartida admite hasta 2000 caracteres."];
        }

        if (estimatedGuestCount < 0)
        {
            errors["estimatedGuestCount"] =
                ["La cantidad estimada de invitados no puede ser negativa."];
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateRequiredText(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            errors[key] =
                [$"El {label} es obligatorio y admite hasta {maxLength} caracteres."];
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

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
