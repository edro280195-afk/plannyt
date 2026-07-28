using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Access.Security;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Application;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Access.Application;

public sealed class InvitationService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    InvitationTokenService invitationTokenService,
    IPasswordHasher<UserAccount> passwordHasher,
    TokenService tokenService,
    AuditService auditService,
    IOptions<FrontendOptions> frontendOptions,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

    public async Task<InvitationCreatedResponse> CreateOrganizationInvitationAsync(
        Guid organizationId,
        CreateOrganizationInvitationRequest request,
        CancellationToken cancellationToken)
    {
        InvitationRequestValidator.ValidateTargetEmail(request.TargetEmail);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationMembersInvite,
            null,
            cancellationToken);
        if (request.IntendedOrganizationRole == OrganizationRole.Owner)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["intendedOrganizationRole"] =
                        ["El rol Owner no se asigna mediante invitación."]
                });
        }

        EnsureCanDelegate(
            access.Permissions,
            RolePermissionCatalog.GetFor(request.IntendedOrganizationRole));
        var targetEmail = request.TargetEmail.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(targetEmail);
        await EnsureNotSelfInvitationAsync(
            access.UserAccountId,
            normalizedEmail,
            cancellationToken);

        var existingAccountId = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(entity => entity.NormalizedEmail == normalizedEmail)
            .Select(entity => (Guid?)entity.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingAccountId is Guid accountId
            && await dbContext.OrganizationMemberships.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.UserAccountId == accountId
                    && entity.Status == MembershipStatus.Active,
                cancellationToken))
        {
            throw new ConflictException(
                "La cuenta ya tiene una membresía activa en la organización.");
        }

        var now = timeProvider.GetUtcNow();
        await RevokePendingInvitationsAsync(
            organizationId,
            null,
            InvitationType.OrganizationMembership,
            normalizedEmail,
            now,
            cancellationToken);
        var token = invitationTokenService.Create();
        var invitation = AccessInvitation.CreateOrganizationMembership(
            organizationId,
            request.IntendedOrganizationRole,
            targetEmail,
            normalizedEmail,
            token.TokenHash,
            now.Add(DefaultLifetime),
            access.UserAccountId,
            now);
        dbContext.AccessInvitations.Add(invitation);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "invitation.organization_created",
            nameof(AccessInvitation),
            invitation.Id,
            new Dictionary<string, object?>
            {
                ["targetEmail"] = targetEmail,
                ["role"] = request.IntendedOrganizationRole.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCreatedResponse(invitation, token.Token);
    }

    public async Task<InvitationCreatedResponse> CreateEventInvitationAsync(
        Guid organizationId,
        Guid eventId,
        CreateEventInvitationRequest request,
        CancellationToken cancellationToken)
    {
        InvitationRequestValidator.ValidateTargetEmail(request.TargetEmail);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsMembersInvite,
            eventId,
            cancellationToken);
        EnsureCanDelegate(
            access.Permissions,
            RolePermissionCatalog.GetFor(request.IntendedEventRole));
        var eventExists = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId
                    && entity.Status != EventStatus.Archived,
                cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException("No se encontró un evento activo.");
        }

        var targetEmail = request.TargetEmail.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(targetEmail);
        await EnsureNotSelfInvitationAsync(
            access.UserAccountId,
            normalizedEmail,
            cancellationToken);
        var existingAccountId = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(entity => entity.NormalizedEmail == normalizedEmail)
            .Select(entity => (Guid?)entity.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingAccountId is Guid accountId
            && await dbContext.EventAccesses.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.UserAccountId == accountId
                    && entity.Status != EventAccessStatus.Revoked
                    && entity.RevokedAt == null,
                cancellationToken))
        {
            throw new ConflictException(
                "La cuenta ya tiene acceso activo al evento.");
        }

        var now = timeProvider.GetUtcNow();
        await RevokePendingInvitationsAsync(
            organizationId,
            eventId,
            InvitationType.EventAccess,
            normalizedEmail,
            now,
            cancellationToken);
        var token = invitationTokenService.Create();
        var invitation = AccessInvitation.CreateEventAccess(
            organizationId,
            eventId,
            request.IntendedEventRole,
            targetEmail,
            normalizedEmail,
            token.TokenHash,
            now.Add(DefaultLifetime),
            access.UserAccountId,
            now);
        dbContext.AccessInvitations.Add(invitation);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation.event_created",
            nameof(AccessInvitation),
            invitation.Id,
            new Dictionary<string, object?>
            {
                ["targetEmail"] = targetEmail,
                ["role"] = request.IntendedEventRole.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCreatedResponse(invitation, token.Token);
    }

    public async Task<InvitationPublicResponse> GetPublicAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = invitationTokenService.Hash(token);
        if (string.IsNullOrEmpty(tokenHash))
        {
            throw new NotFoundException("No se encontró la invitación.");
        }

        var invitation = await dbContext.AccessInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TokenHash == tokenHash,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la invitación.");
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == invitation.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        string? eventName = null;
        if (invitation.EventId is Guid eventId)
        {
            eventName = await dbContext.Events
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == invitation.OrganizationId
                    && entity.Id == eventId)
                .Select(entity => entity.Name)
                .SingleAsync(cancellationToken);
        }

        return new InvitationPublicResponse(
            invitation.InvitationType,
            organizationName,
            eventName,
            invitation.TargetEmail,
            GetIntendedRole(invitation),
            invitation.ExpiresAt,
            GetPublicStatus(invitation, timeProvider.GetUtcNow()));
    }

    public async Task<InvitationAcceptanceResponse> AcceptAsync(
        string token,
        Guid userAccountId,
        AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var invitation = await FindForUpdateAsync(token, cancellationToken);
        EnsureInvitationCanBeAccepted(invitation);
        var account = await dbContext.UserAccounts.SingleAsync(
            entity => entity.Id == userAccountId,
            cancellationToken);
        if (account.NormalizedEmail != invitation.NormalizedTargetEmail)
        {
            throw new ForbiddenException(
                "El correo de la cuenta no coincide con la invitación.");
        }

        var now = timeProvider.GetUtcNow();
        await CreateGrantedAccessAsync(
            invitation,
            account,
            ToProfile(request),
            now,
            cancellationToken);
        invitation.Accept(now);
        auditService.Add(
            invitation.OrganizationId,
            invitation.EventId,
            account.Id,
            "invitation.accepted",
            nameof(AccessInvitation),
            invitation.Id,
            new Dictionary<string, object?>
            {
                ["invitationType"] = invitation.InvitationType.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAcceptanceResponse(invitation);
    }

    public async Task<AuthSessionResult> RegisterAndAcceptAsync(
        string token,
        RegisterAndAcceptInvitationRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        InvitationRequestValidator.Validate(request);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var invitation = await FindForUpdateAsync(token, cancellationToken);
        EnsureInvitationCanBeAccepted(invitation);

        if (await dbContext.UserAccounts.AnyAsync(
            entity => entity.NormalizedEmail == invitation.NormalizedTargetEmail,
            cancellationToken))
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese correo. Inicia sesión para aceptar.");
        }

        var now = timeProvider.GetUtcNow();
        var account = UserAccount.Create(
            invitation.TargetEmail,
            invitation.NormalizedTargetEmail,
            string.Empty,
            now);
        account.SetPasswordHash(
            passwordHasher.HashPassword(account, request.Password),
            now);
        dbContext.UserAccounts.Add(account);
        await CreateGrantedAccessAsync(
            invitation,
            account,
            ToProfile(request),
            now,
            cancellationToken);
        invitation.Accept(now);

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
        dbContext.UserSessions.Add(session);
        auditService.Add(
            invitation.OrganizationId,
            invitation.EventId,
            account.Id,
            "invitation.registered_and_accepted",
            nameof(AccessInvitation),
            invitation.Id,
            new Dictionary<string, object?>
            {
                ["invitationType"] = invitation.InvitationType.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var accessToken = tokenService.CreateAccessToken(account, session);
        return new AuthSessionResult(
            new AuthResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                account.Id,
                account.Email,
                invitation.InvitationType == InvitationType.OrganizationMembership
                    ? invitation.OrganizationId
                    : null),
            refresh.Token,
            refresh.ExpiresAt,
            true);
    }

    public async Task RevokeOrganizationInvitationAsync(
        Guid organizationId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationMembersInvite,
            null,
            cancellationToken);
        await RevokeInvitationAsync(
            organizationId,
            null,
            invitationId,
            InvitationType.OrganizationMembership,
            access.UserAccountId,
            cancellationToken);
    }

    public async Task RevokeEventInvitationAsync(
        Guid organizationId,
        Guid eventId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsMembersInvite,
            eventId,
            cancellationToken);
        await RevokeInvitationAsync(
            organizationId,
            eventId,
            invitationId,
            InvitationType.EventAccess,
            access.UserAccountId,
            cancellationToken);
    }

    private async Task RevokeInvitationAsync(
        Guid organizationId,
        Guid? eventId,
        Guid invitationId,
        InvitationType invitationType,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var invitation = await dbContext.AccessInvitations.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == invitationId
                && entity.InvitationType == invitationType,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró la invitación.");
        if (invitation.AcceptedAt is not null)
        {
            throw new ConflictException(
                "Una invitación aceptada ya no puede revocarse.");
        }

        if (invitation.RevokedAt is not null)
        {
            throw new ConflictException("La invitación ya está revocada.");
        }

        invitation.Revoke(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            actorUserId,
            "invitation.revoked",
            nameof(AccessInvitation),
            invitation.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateGrantedAccessAsync(
        AccessInvitation invitation,
        UserAccount account,
        ProfileData? profile,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organizationIsActive = await dbContext.Organizations.AnyAsync(
            entity =>
                entity.Id == invitation.OrganizationId
                && entity.Status == OrganizationStatus.Active,
            cancellationToken);
        if (!organizationIsActive)
        {
            throw new GoneException("La organización ya no está activa.");
        }

        var person = await dbContext.People.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == invitation.OrganizationId
                && entity.LinkedUserAccountId == account.Id
                && entity.ArchivedAt == null,
            cancellationToken);

        if (invitation.InvitationType == InvitationType.OrganizationMembership)
        {
            if (await dbContext.OrganizationMemberships.AnyAsync(
                entity =>
                    entity.OrganizationId == invitation.OrganizationId
                    && entity.UserAccountId == account.Id
                    && entity.Status == MembershipStatus.Active,
                cancellationToken))
            {
                throw new ConflictException(
                    "La cuenta ya tiene una membresía activa.");
            }

            if (person is null)
            {
                if (profile is null)
                {
                    throw new RequestValidationException(
                        new Dictionary<string, string[]>
                        {
                            ["firstName"] =
                                ["Se requiere el perfil para crear la membresía."]
                        });
                }

                InvitationRequestValidator.ValidateRequiredProfile(
                    profile.ToAcceptRequest());
                person = CreatePerson(invitation.OrganizationId, account, profile, now);
                dbContext.People.Add(person);
            }

            var membership = OrganizationMembership.Create(
                invitation.OrganizationId,
                account.Id,
                person.Id,
                invitation.IntendedOrganizationRole
                    ?? throw new InvalidOperationException(
                        "La invitación no contiene rol de organización."),
                now,
                null,
                now);
            dbContext.OrganizationMemberships.Add(membership);
            return;
        }

        var eventId = invitation.EventId
            ?? throw new InvalidOperationException(
                "La invitación no contiene evento.");
        var eventIsActive = await dbContext.Events.AnyAsync(
            entity =>
                entity.OrganizationId == invitation.OrganizationId
                && entity.Id == eventId
                && entity.Status != EventStatus.Archived,
            cancellationToken);
        if (!eventIsActive)
        {
            throw new GoneException("El evento ya no está disponible.");
        }

        if (await dbContext.EventAccesses.AnyAsync(
            entity =>
                entity.OrganizationId == invitation.OrganizationId
                && entity.EventId == eventId
                && entity.UserAccountId == account.Id
                && entity.Status != EventAccessStatus.Revoked
                && entity.RevokedAt == null,
            cancellationToken))
        {
            throw new ConflictException("La cuenta ya tiene acceso al evento.");
        }

        if (person is null && profile is not null)
        {
            InvitationRequestValidator.ValidateRequiredProfile(
                profile.ToAcceptRequest());
            person = CreatePerson(invitation.OrganizationId, account, profile, now);
            dbContext.People.Add(person);
        }

        dbContext.EventAccesses.Add(EventAccess.CreateAccepted(
            invitation.OrganizationId,
            eventId,
            account.Id,
            invitation.IntendedEventRole
                ?? throw new InvalidOperationException(
                    "La invitación no contiene rol de evento."),
            now,
            null,
            invitation.InvitedBy,
            now,
            now));
    }

    private async Task<AccessInvitation> FindForUpdateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = invitationTokenService.Hash(token);
        if (string.IsNullOrEmpty(tokenHash))
        {
            throw new NotFoundException("No se encontró la invitación.");
        }

        return await dbContext.AccessInvitations
            .FromSqlInterpolated(
                $"SELECT * FROM access_invitations WHERE token_hash = {tokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró la invitación.");
    }

    private void EnsureInvitationCanBeAccepted(AccessInvitation invitation)
    {
        if (!invitation.CanAcceptAt(timeProvider.GetUtcNow()))
        {
            throw new GoneException(
                "La invitación venció, fue utilizada o está revocada.");
        }
    }

    private async Task RevokePendingInvitationsAsync(
        Guid organizationId,
        Guid? eventId,
        InvitationType invitationType,
        string normalizedEmail,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pending = await dbContext.AccessInvitations
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationType == invitationType
                && entity.NormalizedTargetEmail == normalizedEmail
                && entity.AcceptedAt == null
                && entity.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var invitation in pending)
        {
            invitation.Revoke(now);
        }
    }

    private async Task EnsureNotSelfInvitationAsync(
        Guid actorUserId,
        string normalizedTargetEmail,
        CancellationToken cancellationToken)
    {
        var actorEmail = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(entity => entity.Id == actorUserId)
            .Select(entity => entity.NormalizedEmail)
            .SingleAsync(cancellationToken);
        if (actorEmail == normalizedTargetEmail)
        {
            throw new ForbiddenException(
                "No puedes enviarte una invitación para modificar tu propio acceso.");
        }
    }

    private static void EnsureCanDelegate(
        IReadOnlySet<string> actorPermissions,
        IReadOnlySet<string> intendedPermissions)
    {
        if (!intendedPermissions.IsSubsetOf(actorPermissions))
        {
            throw new ForbiddenException(
                "No puedes delegar permisos que no posees.");
        }
    }

    private InvitationCreatedResponse ToCreatedResponse(
        AccessInvitation invitation,
        string rawToken)
    {
        var publicUrl = frontendOptions.Value.PublicUrl.TrimEnd('/');
        return new InvitationCreatedResponse(
            invitation.Id,
            invitation.InvitationType,
            invitation.TargetEmail,
            invitation.ExpiresAt,
            $"{publicUrl}/accept-access/{rawToken}");
    }

    private static InvitationAcceptanceResponse ToAcceptanceResponse(
        AccessInvitation invitation) =>
        new(
            invitation.InvitationType,
            invitation.InvitationType == InvitationType.OrganizationMembership
                ? invitation.OrganizationId
                : null,
            invitation.EventId);

    private static InvitationPublicStatus GetPublicStatus(
        AccessInvitation invitation,
        DateTimeOffset now)
    {
        if (invitation.AcceptedAt is not null)
        {
            return InvitationPublicStatus.Accepted;
        }

        if (invitation.RevokedAt is not null)
        {
            return InvitationPublicStatus.Revoked;
        }

        return invitation.ExpiresAt <= now
            ? InvitationPublicStatus.Expired
            : InvitationPublicStatus.Pending;
    }

    private static string GetIntendedRole(AccessInvitation invitation) =>
        invitation.IntendedOrganizationRole?.ToString()
        ?? invitation.IntendedEventRole?.ToString()
        ?? string.Empty;

    private static Person CreatePerson(
        Guid organizationId,
        UserAccount account,
        ProfileData profile,
        DateTimeOffset now) =>
        Person.Create(
            organizationId,
            account.Id,
            profile.FirstName.Trim(),
            profile.LastName.Trim(),
            $"{profile.FirstName.Trim()} {profile.LastName.Trim()}",
            account.Email,
            Normalize(profile.ContactPhone),
            profile.PreferredLanguage.Trim(),
            profile.TimeZone.Trim(),
            now);

    private static ProfileData? ToProfile(AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName)
            && string.IsNullOrWhiteSpace(request.LastName)
            && string.IsNullOrWhiteSpace(request.PreferredLanguage)
            && string.IsNullOrWhiteSpace(request.TimeZone)
            && string.IsNullOrWhiteSpace(request.ContactPhone))
        {
            return null;
        }

        return new ProfileData(
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.ContactPhone,
            request.PreferredLanguage ?? string.Empty,
            request.TimeZone ?? string.Empty);
    }

    private static ProfileData ToProfile(
        RegisterAndAcceptInvitationRequest request) =>
        new(
            request.FirstName,
            request.LastName,
            request.ContactPhone,
            request.PreferredLanguage,
            request.TimeZone);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Limit(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    private sealed record ProfileData(
        string FirstName,
        string LastName,
        string? ContactPhone,
        string PreferredLanguage,
        string TimeZone)
    {
        public AcceptInvitationRequest ToAcceptRequest() =>
            new(
                FirstName,
                LastName,
                ContactPhone,
                PreferredLanguage,
                TimeZone);
    }
}
