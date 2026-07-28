using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Proposals.Application;

public static class ProposalRequestValidator
{
    public static void Validate(ProposalDraftRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ProspectId is null && request.ClientId is null)
        {
            errors["target"] = ["Selecciona un prospecto o cliente."];
        }

        if (request.CurrencyCode?.Trim().Length != 3)
        {
            errors["currencyCode"] =
                ["La moneda debe indicarse con tres letras."];
        }

        Optional(errors, "sharedIntroduction", request.SharedIntroduction, 4000);
        Optional(errors, "sharedTerms", request.SharedTerms, 8000);
        Optional(errors, "internalNotes", request.InternalNotes, 4000);
        ValidateDiscount(
            errors,
            "generalDiscountValue",
            request.GeneralDiscountType,
            request.GeneralDiscountValue);

        if (request.Lines.Count == 0)
        {
            errors["lines"] = ["Agrega al menos un concepto."];
        }

        if (request.Lines.All(line => line.IsOptional))
        {
            errors["lines"] =
                ["Agrega al menos un concepto no opcional."];
        }

        for (var index = 0; index < request.Lines.Count; index++)
        {
            var line = request.Lines[index];
            if (string.IsNullOrWhiteSpace(line.Description)
                || line.Description.Trim().Length > 1000)
            {
                errors[$"lines[{index}].description"] =
                    ["La descripción es obligatoria y admite hasta 1000 caracteres."];
            }

            if (line.ServiceCatalogItemId is not null && line.PackageId is not null)
            {
                errors[$"lines[{index}]"] =
                    ["Una línea no puede referir servicio y paquete al mismo tiempo."];
            }

            if (line.Quantity <= 0)
            {
                errors[$"lines[{index}].quantity"] =
                    ["La cantidad debe ser mayor que cero."];
            }

            if (line.UnitPrice < 0)
            {
                errors[$"lines[{index}].unitPrice"] =
                    ["El precio no puede ser negativo."];
            }

            if (line.TaxRate is < 0 or > 100)
            {
                errors[$"lines[{index}].taxRate"] =
                    ["El impuesto debe estar entre 0 y 100."];
            }

            ValidateDiscount(
                errors,
                $"lines[{index}].discountValue",
                line.DiscountType,
                line.DiscountValue);
        }

        Throw(errors);
    }

    public static void Validate(CreateProposalCommentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "authorDisplayName", request.AuthorDisplayName, 160);
        Required(errors, "content", request.Content, 4000);
        Throw(errors);
    }

    public static void Validate(ProposalPublicCommentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "authorDisplayName", request.AuthorDisplayName, 160);
        Required(errors, "content", request.Content, 4000);
        Throw(errors);
    }

    private static void ValidateDiscount(
        Dictionary<string, string[]> errors,
        string key,
        DiscountType type,
        decimal value)
    {
        if (value < 0)
        {
            errors[key] = ["El descuento no puede ser negativo."];
        }
        else if (type == DiscountType.Percentage && value > 100)
        {
            errors[key] = ["El porcentaje no puede exceder 100."];
        }
        else if (type == DiscountType.None && value != 0)
        {
            errors[key] =
                ["Un descuento de tipo None debe tener valor cero."];
        }
    }

    private static void Required(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            errors[key] =
                [$"El campo es obligatorio y admite hasta {maxLength} caracteres."];
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
