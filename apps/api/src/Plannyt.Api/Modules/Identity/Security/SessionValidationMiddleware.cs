using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Infrastructure.Persistence;

namespace Plannyt.Api.Modules.Identity.Security;

public sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        PlannytDbContext dbContext,
        TimeProvider timeProvider)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var sessionIdValue = context.User.FindFirstValue("sid");
        var securityVersionValue = context.User.FindFirstValue("security_version");

        if (!Guid.TryParse(userIdValue, out var userId)
            || !Guid.TryParse(sessionIdValue, out var sessionId)
            || !int.TryParse(securityVersionValue, out var securityVersion))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var session = await dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == sessionId);
        var account = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId);
        var now = timeProvider.GetUtcNow();

        if (session is null
            || account is null
            || session.UserAccountId != account.Id
            || !session.IsActiveAt(now)
            || !account.IsActive
            || account.SecurityVersion != securityVersion
            || session.SecurityVersionAtCreation != account.SecurityVersion)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        await next(context);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new
        {
            title = "La sesión no está vigente.",
            status = StatusCodes.Status401Unauthorized,
            correlationId = context.TraceIdentifier
        });
    }
}
