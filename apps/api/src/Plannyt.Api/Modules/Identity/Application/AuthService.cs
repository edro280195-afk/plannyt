using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Identity.Application;

public sealed class AuthService(
    PlannytDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    TokenService tokenService,
    OrganizationSlugGenerator slugGenerator,
    AuditService auditService,
    TimeProvider timeProvider)
{
    private const string InvalidCredentialsMessage =
        "El correo o la contraseña no son válidos.";

    public async Task<AuthSessionResult> RegisterPlannerAsync(
        RegisterPlannerRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        AuthRequestValidator.Validate(request);
        var now = timeProvider.GetUtcNow();
        var email = request.Email.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(email);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (await dbContext.UserAccounts.AnyAsync(
                entity => entity.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese correo electrónico.");
        }

        var account = UserAccount.Create(email, normalizedEmail, string.Empty, now);
        account.SetPasswordHash(
            passwordHasher.HashPassword(account, request.Password),
            now);
        var organization = Organization.Create(
            request.OrganizationName.Trim(),
            await slugGenerator.GenerateAsync(
                request.OrganizationName,
                cancellationToken),
            request.OrganizationType,
            request.TimeZone.Trim(),
            request.CountryCode.Trim().ToUpperInvariant(),
            request.CurrencyCode.Trim().ToUpperInvariant(),
            now);
        var person = Person.Create(
            organization.Id,
            account.Id,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            email,
            null,
            "es",
            request.TimeZone.Trim(),
            now);
        var membership = OrganizationMembership.CreateOwner(
            organization.Id,
            account.Id,
            person.Id,
            now);
        var refresh = tokenService.CreateRefreshToken();
        var session = UserSession.Create(
            account.Id,
            refresh.TokenHash,
            now,
            refresh.ExpiresAt,
            Limit(ipAddress, 64),
            Limit(userAgent, 512),
            true,
            account.SecurityVersion);

        dbContext.AddRange(account, organization, person, membership, session);
        auditService.Add(
            organization.Id,
            null,
            account.Id,
            "auth.planner_registered",
            nameof(UserAccount),
            account.Id,
            new Dictionary<string, object?>
            {
                ["organizationId"] = organization.Id,
                ["membershipId"] = membership.Id
            });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException(
                $"No fue posible completar el registro: {GetSafeDatabaseConflict(exception)}");
        }

        return await BuildSessionResultAsync(
            account,
            session,
            refresh,
            true,
            organization.Id,
            cancellationToken);
    }

    public async Task<AuthSessionResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        AuthRequestValidator.Validate(request);
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);
        var account = await dbContext.UserAccounts.SingleOrDefaultAsync(
            entity => entity.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (account is null || !account.IsActive)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var verification = passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var now = timeProvider.GetUtcNow();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.SetPasswordHash(
                passwordHasher.HashPassword(account, request.Password),
                now);
        }

        account.RecordLogin(now);
        var refresh = tokenService.CreateRefreshToken();
        var session = UserSession.Create(
            account.Id,
            refresh.TokenHash,
            now,
            refresh.ExpiresAt,
            Limit(ipAddress, 64),
            Limit(userAgent, 512),
            request.IsPersistent,
            account.SecurityVersion);
        dbContext.UserSessions.Add(session);
        var organizationId = await FindPrimaryOrganizationIdAsync(
            account.Id,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            null,
            account.Id,
            "auth.login_succeeded",
            nameof(UserSession),
            session.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildSessionResultAsync(
            account,
            session,
            refresh,
            request.IsPersistent,
            organizationId,
            cancellationToken);
    }

    public async Task<AuthSessionResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("La sesión de renovación no es válida.");
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = tokenService.HashRefreshToken(refreshToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(
            entity => entity.RefreshTokenHash == tokenHash,
            cancellationToken);

        if (session is null)
        {
            throw new UnauthorizedException("La sesión de renovación no es válida.");
        }

        var account = await dbContext.UserAccounts.SingleAsync(
            entity => entity.Id == session.UserAccountId,
            cancellationToken);
        var organizationId = await FindPrimaryOrganizationIdAsync(
            account.Id,
            now,
            cancellationToken);

        if (session.ReplacedBySessionId is not null)
        {
            await RevokeRotatedChainAsync(
                session,
                now,
                cancellationToken);
            auditService.Add(
                organizationId,
                null,
                account.Id,
                "auth.refresh_reuse_detected",
                nameof(UserSession),
                session.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException(
                "La sesión fue revocada por reutilización del token.");
        }

        if (!session.IsActiveAt(now)
            || !account.IsActive
            || session.SecurityVersionAtCreation != account.SecurityVersion)
        {
            throw new UnauthorizedException("La sesión de renovación no es válida.");
        }

        var replacementToken = tokenService.CreateRefreshToken();
        var replacementSession = UserSession.Create(
            account.Id,
            replacementToken.TokenHash,
            now,
            replacementToken.ExpiresAt,
            Limit(ipAddress, 64),
            Limit(userAgent, 512),
            session.IsPersistent,
            account.SecurityVersion);
        session.MarkUsed(now, Limit(ipAddress, 64));
        session.Revoke(now, "Rotated", replacementSession.Id);
        dbContext.UserSessions.Add(replacementSession);
        auditService.Add(
            organizationId,
            null,
            account.Id,
            "auth.session_refreshed",
            nameof(UserSession),
            replacementSession.Id,
            new Dictionary<string, object?>
            {
                ["replacedSessionId"] = session.Id
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildSessionResultAsync(
            account,
            replacementSession,
            replacementToken,
            session.IsPersistent,
            organizationId,
            cancellationToken);
    }

    public async Task LogoutAsync(
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(
            entity => entity.RefreshTokenHash == tokenHash,
            cancellationToken);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        session.Revoke(now, "Logout");
        var organizationId = await FindPrimaryOrganizationIdAsync(
            session.UserAccountId,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            null,
            session.UserAccountId,
            "auth.logout",
            nameof(UserSession),
            session.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var account = await dbContext.UserAccounts.SingleAsync(
            entity => entity.Id == userAccountId,
            cancellationToken);
        var sessions = await dbContext.UserSessions
            .Where(entity =>
                entity.UserAccountId == userAccountId
                && entity.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now, "LogoutAll");
        }

        account.IncrementSecurityVersion(now);
        var organizationId = await FindPrimaryOrganizationIdAsync(
            account.Id,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            null,
            account.Id,
            "auth.logout_all",
            nameof(UserAccount),
            account.Id,
            new Dictionary<string, object?>
            {
                ["revokedSessionCount"] = sessions.Count
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeResponse> GetMeAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var account = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == userAccountId, cancellationToken);
        var memberships = await dbContext.OrganizationMemberships
            .AsNoTracking()
            .Where(entity =>
                entity.UserAccountId == userAccountId
                && entity.Status == MembershipStatus.Active
                && entity.JoinedAt <= now
                && (entity.ExpiresAt == null || entity.ExpiresAt > now))
            .Join(
                dbContext.Organizations.AsNoTracking(),
                membership => membership.OrganizationId,
                organization => organization.Id,
                (membership, organization) => new
                {
                    Membership = membership,
                    OrganizationName = organization.Name
                })
            .ToListAsync(cancellationToken);
        var organizations = new List<MeOrganizationResponse>(memberships.Count);

        foreach (var item in memberships)
        {
            var grants = await dbContext.PermissionGrants
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == item.Membership.OrganizationId
                    && entity.Scope == PermissionScope.Organization
                    && (entity.UserAccountId == userAccountId
                        || entity.OrganizationMembershipId == item.Membership.Id))
                .ToListAsync(cancellationToken);
            var permissions = EffectivePermissionResolver.Resolve(
                RolePermissionCatalog.GetFor(item.Membership.BaseRole),
                grants,
                now);
            organizations.Add(new MeOrganizationResponse(
                item.Membership.OrganizationId,
                item.OrganizationName,
                item.Membership.Id,
                item.Membership.BaseRole,
                permissions));
        }

        var eventAccesses = await dbContext.EventAccesses
            .AsNoTracking()
            .Where(entity =>
                entity.UserAccountId == userAccountId
                && entity.Status == EventAccessStatus.Active
                && entity.StartsAt <= now
                && (entity.ExpiresAt == null || entity.ExpiresAt > now)
                && entity.RevokedAt == null)
            .Join(
                dbContext.Events.AsNoTracking(),
                access => new { access.OrganizationId, access.EventId },
                eventEntity => new
                {
                    eventEntity.OrganizationId,
                    EventId = eventEntity.Id
                },
                (access, eventEntity) => new MeEventAccessResponse(
                    access.OrganizationId,
                    access.EventId,
                    eventEntity.Name,
                    access.BaseRole.ToString()))
            .ToListAsync(cancellationToken);

        return new MeResponse(account.Id, account.Email, organizations, eventAccesses);
    }

    private async Task<AuthSessionResult> BuildSessionResultAsync(
        UserAccount account,
        UserSession session,
        RefreshTokenResult refreshToken,
        bool isPersistent,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var accessToken = tokenService.CreateAccessToken(account, session);
        return await Task.FromResult(new AuthSessionResult(
            new AuthResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                account.Id,
                account.Email,
                organizationId),
            refreshToken.Token,
            refreshToken.ExpiresAt,
            isPersistent));
    }

    private async Task RevokeRotatedChainAsync(
        UserSession firstSession,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.UserSessions
            .Where(entity => entity.UserAccountId == firstSession.UserAccountId)
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);
        var current = firstSession;

        while (current.ReplacedBySessionId is Guid replacementId
               && sessions.TryGetValue(replacementId, out var replacement))
        {
            replacement.Revoke(now, "RefreshTokenReuseDetected");
            current = replacement;
        }
    }

    private Task<Guid?> FindPrimaryOrganizationIdAsync(
        Guid userAccountId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMemberships
            .AsNoTracking()
            .Where(entity =>
                entity.UserAccountId == userAccountId
                && entity.Status == MembershipStatus.Active
                && entity.JoinedAt <= now
                && (entity.ExpiresAt == null || entity.ExpiresAt > now))
            .OrderBy(entity => entity.JoinedAt)
            .Select(entity => (Guid?)entity.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? Limit(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    private static string GetSafeDatabaseConflict(DbUpdateException exception)
    {
        _ = exception;
        return "los datos ya existen o no cumplen una restricción.";
    }
}
