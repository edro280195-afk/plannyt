using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.BuildingBlocks.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            ApiException apiException => apiException.StatusCode,
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            DomainRuleException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        var title = exception switch
        {
            ApiException apiException => apiException.Title,
            BadHttpRequestException => "La solicitud no es válida",
            DomainRuleException => "La operación no es válida",
            _ => "Ocurrió un error inesperado"
        };
        var detail = exception switch
        {
            ApiException or DomainRuleException => exception.Message,
            BadHttpRequestException =>
                "No se pudo interpretar el contenido de la solicitud.",
            _ => "La operación no pudo completarse."
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Error no controlado. CorrelationId: {CorrelationId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                "Solicitud rechazada con estado {StatusCode}. CorrelationId: {CorrelationId}",
                statusCode,
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is RsvpValidationException rsvpValidationException)
        {
            problemDetails.Type =
                "https://plannyt.com/problems/rsvp-validation";
            problemDetails.Extensions["errors"] =
                rsvpValidationException.Errors;
        }

        if (exception is RequestValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        if (exception is PublicInvitationUnavailableException unavailableException)
        {
            problemDetails.Extensions["reason"] = unavailableException.Reason;
        }

        if (exception is RsvpRevisionConflictException revisionConflict)
        {
            problemDetails.Extensions["expectedRevision"] =
                revisionConflict.ExpectedRevision;
            problemDetails.Extensions["currentRevision"] =
                revisionConflict.CurrentRevision;
            problemDetails.Extensions["reloadRequired"] = true;
        }

        if (exception is IdempotencyConflictException)
        {
            problemDetails.Extensions["conflictType"] =
                "idempotency-key-reused-with-different-content";
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
