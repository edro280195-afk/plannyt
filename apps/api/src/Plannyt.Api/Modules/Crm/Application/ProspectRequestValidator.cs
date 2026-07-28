using System.Net.Mail;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Crm.Application;

public static class ProspectRequestValidator
{
    public static void Validate(ProspectDetailsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "displayName", request.DisplayName, 200, "nombre");
        Optional(errors, "firstName", request.FirstName, 100);
        Optional(errors, "lastName", request.LastName, 100);
        Optional(errors, "companyName", request.CompanyName, 200);
        Optional(errors, "phone", request.Phone, 40);
        Optional(errors, "source", request.Source, 100);
        Optional(errors, "eventTypeInterest", request.EventTypeInterest, 80);
        Optional(errors, "city", request.City, 120);
        Optional(errors, "notes", request.Notes, 4000);

        if (!string.IsNullOrWhiteSpace(request.Email)
            && (!MailAddress.TryCreate(request.Email.Trim(), out _)
                || request.Email.Trim().Length > 254))
        {
            errors["email"] = ["El correo no es válido."];
        }

        if (request.EstimatedGuestCount < 0)
        {
            errors["estimatedGuestCount"] =
                ["La cantidad estimada de invitados no puede ser negativa."];
        }

        if (request.EstimatedBudget < 0)
        {
            errors["estimatedBudget"] =
                ["El presupuesto estimado no puede ser negativo."];
        }

        if (request.CurrencyCode?.Trim().Length != 3)
        {
            errors["currencyCode"] =
                ["La moneda debe indicarse con un código de tres letras."];
        }

        Throw(errors);
    }

    public static void Validate(CreateProspectActivityRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "subject", request.Subject, 200, "asunto");
        Optional(errors, "description", request.Description, 4000);
        if (request.CompletedAt < request.ScheduledAt)
        {
            errors["completedAt"] =
                ["La conclusión no puede ser anterior al seguimiento."];
        }

        Throw(errors);
    }

    public static void Validate(LinkPreliminaryEventRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ExistingEventId is not null)
        {
            if (request.Name is not null
                || request.EventType is not null
                || request.StartDateTime is not null)
            {
                errors["existingEventId"] =
                    ["Al relacionar un evento existente no envíes datos de uno nuevo."];
            }
        }
        else
        {
            Required(errors, "name", request.Name, 200, "nombre");
            Required(errors, "eventType", request.EventType, 80, "tipo de evento");
            Required(errors, "timeZone", request.TimeZone, 100, "zona horaria");
            Required(errors, "city", request.City, 120, "ciudad");
            if (request.StartDateTime is null)
            {
                errors["startDateTime"] = ["La fecha estimada es obligatoria."];
            }

            if (request.CountryCode?.Trim().Length != 2)
            {
                errors["countryCode"] =
                    ["El país debe indicarse con un código de dos letras."];
            }
        }

        if (request.EstimatedGuestCount < 0)
        {
            errors["estimatedGuestCount"] =
                ["La cantidad de invitados no puede ser negativa."];
        }

        Throw(errors);
    }

    private static void Required(
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

    private static void Optional(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors[key] = [$"El campo admite hasta {maxLength} caracteres."];
        }
    }

    private static void Throw(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
