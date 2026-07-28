using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Catalog.Application;

public static class CatalogRequestValidator
{
    public static void Validate(ServiceCatalogItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "name", request.Name, 200);
        Required(errors, "category", request.Category, 100);
        Optional(errors, "description", request.Description, 2000);
        Currency(errors, request.CurrencyCode);
        NonNegative(errors, "basePrice", request.BasePrice);
        if (request.SortOrder < 0)
        {
            errors["sortOrder"] = ["El orden no puede ser negativo."];
        }

        Throw(errors);
    }

    public static void Validate(PackageRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, "name", request.Name, 200);
        Optional(errors, "description", request.Description, 2000);
        Currency(errors, request.CurrencyCode);
        NonNegative(errors, "basePrice", request.BasePrice);
        var duplicateServices = request.Items
            .GroupBy(item => item.ServiceCatalogItemId)
            .Any(group => group.Count() > 1);
        if (duplicateServices)
        {
            errors["items"] =
                ["Un servicio solo puede aparecer una vez dentro del paquete."];
        }

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            if (item.Quantity <= 0)
            {
                errors[$"items[{index}].quantity"] =
                    ["La cantidad debe ser mayor que cero."];
            }

            if (item.IncludedPrice < 0)
            {
                errors[$"items[{index}].includedPrice"] =
                    ["El precio incluido no puede ser negativo."];
            }
        }

        Throw(errors);
    }

    public static void Validate(CouponRequest request, bool isCreate)
    {
        var errors = new Dictionary<string, string[]>();
        if (isCreate)
        {
            Required(errors, "code", request.Code, 40);
        }

        Optional(errors, "description", request.Description, 500);
        if (request.DiscountType == DiscountType.None)
        {
            errors["discountType"] =
                ["Selecciona un tipo de descuento para el cupón."];
        }

        NonNegative(errors, "discountValue", request.DiscountValue);
        if (request.DiscountType == DiscountType.Percentage
            && request.DiscountValue > 100)
        {
            errors["discountValue"] =
                ["El porcentaje no puede exceder 100."];
        }

        if (request.EndsAt < request.StartsAt)
        {
            errors["endsAt"] =
                ["La fecha final no puede ser anterior al inicio."];
        }

        if (request.MaximumUses <= 0)
        {
            errors["maximumUses"] =
                ["El máximo de usos debe ser mayor que cero."];
        }

        Throw(errors);
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

    private static void Currency(
        Dictionary<string, string[]> errors,
        string? currencyCode)
    {
        if (currencyCode?.Trim().Length != 3)
        {
            errors["currencyCode"] =
                ["La moneda debe indicarse con tres letras."];
        }
    }

    private static void NonNegative(
        Dictionary<string, string[]> errors,
        string key,
        decimal value)
    {
        if (value < 0)
        {
            errors[key] = ["El valor no puede ser negativo."];
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
