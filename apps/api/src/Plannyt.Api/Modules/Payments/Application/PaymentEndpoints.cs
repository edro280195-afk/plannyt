using Microsoft.AspNetCore.Mvc;
using Plannyt.Api.Modules.Documents.Application;

namespace Plannyt.Api.Modules.Payments.Application;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var plans = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/payment-plans")
            .WithTags("Planes de pago")
            .RequireAuthorization();
        plans.MapGet("/", async (
            Guid organizationId,
            Guid? eventId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPlansAsync(
                organizationId,
                eventId,
                cancellationToken)));
        plans.MapPost("/", async (
            Guid organizationId,
            CreatePaymentPlanRequest request,
            PaymentService service,
            CancellationToken cancellationToken) =>
        {
            var plan = await service.CreatePlanAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/payment-plans/{plan.Id}",
                plan);
        });
        plans.MapGet("/{planId:guid}", async (
            Guid organizationId,
            Guid planId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPlanAsync(
                organizationId,
                planId,
                cancellationToken)));
        plans.MapPut("/{planId:guid}", async (
            Guid organizationId,
            Guid planId,
            CreatePaymentPlanRequest request,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdatePlanAsync(
                organizationId,
                planId,
                request,
                cancellationToken)));
        plans.MapPost("/{planId:guid}/activate", async (
            Guid organizationId,
            Guid planId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ActivatePlanAsync(
                organizationId,
                planId,
                cancellationToken)));
        plans.MapPost("/{planId:guid}/cancel", async (
            Guid organizationId,
            Guid planId,
            PaymentService service,
            CancellationToken cancellationToken) =>
        {
            await service.CancelPlanAsync(
                organizationId,
                planId,
                cancellationToken);
            return Results.NoContent();
        });

        var payments = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/payments")
            .WithTags("Pagos")
            .RequireAuthorization();
        payments.MapGet("/", async (
            Guid organizationId,
            Guid? eventId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPaymentsAsync(
                organizationId,
                eventId,
                cancellationToken)));
        payments.MapPost("/", async (
            Guid organizationId,
            CreatePaymentRecordRequest request,
            PaymentService service,
            CancellationToken cancellationToken) =>
        {
            var payment = await service.CreatePaymentAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/payments/{payment.Id}",
                payment);
        });
        payments.MapPost("/{paymentId:guid}/approve", async (
            Guid organizationId,
            Guid paymentId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ApprovePaymentAsync(
                organizationId,
                paymentId,
                cancellationToken)));
        payments.MapPost("/{paymentId:guid}/reject", async (
            Guid organizationId,
            Guid paymentId,
            RejectPaymentRequest request,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.RejectPaymentAsync(
                organizationId,
                paymentId,
                request,
                cancellationToken)));
        payments.MapPost("/{paymentId:guid}/allocations", async (
            Guid organizationId,
            Guid paymentId,
            IReadOnlyList<PaymentAllocationRequest> request,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.AllocatePaymentAsync(
                organizationId,
                paymentId,
                request,
                cancellationToken)));
        payments.MapPost("/{paymentId:guid}/cancel", async (
            Guid organizationId,
            Guid paymentId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.CancelPaymentAsync(
                organizationId,
                paymentId,
                cancellationToken)));
        payments.MapPost("/{paymentId:guid}/refund", async (
            Guid organizationId,
            Guid paymentId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.RefundPaymentAsync(
                organizationId,
                paymentId,
                cancellationToken)));
        payments.MapPost(
                "/{paymentId:guid}/receipt",
                async (
                    Guid organizationId,
                    Guid paymentId,
                    [FromForm] UploadPaymentReceiptRequest request,
                    PaymentService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.UploadReceiptAsync(
                        organizationId,
                        paymentId,
                        request,
                        cancellationToken)))
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(
                DocumentFileValidator.MaxFileSize + 1024 * 1024));

        var portalPlans = endpoints
            .MapGroup("/api/client-portal/payment-plans")
            .WithTags("Portal del cliente")
            .RequireAuthorization();
        portalPlans.MapGet("/", async (
            Guid? eventId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalPlansAsync(
                eventId,
                cancellationToken)));

        var portalPayments = endpoints
            .MapGroup("/api/client-portal/payments")
            .WithTags("Portal del cliente")
            .RequireAuthorization();
        portalPayments.MapGet("/", async (
            Guid? eventId,
            PaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalPaymentsAsync(
                eventId,
                cancellationToken)));
        portalPayments.MapPost("/", async (
            PortalCreatePaymentRequest request,
            PaymentService service,
            CancellationToken cancellationToken) =>
        {
            var payment = await service.CreatePortalPaymentAsync(
                request,
                cancellationToken);
            return Results.Created(
                $"/api/client-portal/payments/{payment.Id}",
                payment);
        });
        portalPayments.MapPost(
                "/{paymentId:guid}/receipt",
                async (
                    Guid paymentId,
                    [FromForm] UploadPaymentReceiptRequest request,
                    PaymentService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.UploadPortalReceiptAsync(
                        paymentId,
                        request,
                        cancellationToken)))
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(
                DocumentFileValidator.MaxFileSize + 1024 * 1024));
        return endpoints;
    }
}
