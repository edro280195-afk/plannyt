namespace Plannyt.Api.Modules.Identity.Domain;

public sealed class UserSession
{
    private UserSession()
    {
    }

    private UserSession(
        Guid id,
        Guid userAccountId,
        string refreshTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? createdByIp,
        string? userAgent,
        bool isPersistent,
        int securityVersionAtCreation)
    {
        Id = id;
        UserAccountId = userAccountId;
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
        IsPersistent = isPersistent;
        SecurityVersionAtCreation = securityVersionAtCreation;
    }

    public Guid Id { get; private set; }

    public Guid UserAccountId { get; private set; }

    public string RefreshTokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? LastUsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevocationReason { get; private set; }

    public Guid? ReplacedBySessionId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? LastUsedIp { get; private set; }

    public string? UserAgent { get; private set; }

    public bool IsPersistent { get; private set; }

    public int SecurityVersionAtCreation { get; private set; }

    public static UserSession Create(
        Guid userAccountId,
        string refreshTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? createdByIp,
        string? userAgent,
        bool isPersistent,
        int securityVersionAtCreation) =>
        new(
            Guid.NewGuid(),
            userAccountId,
            refreshTokenHash,
            createdAt,
            expiresAt,
            createdByIp,
            userAgent,
            isPersistent,
            securityVersionAtCreation);

    public bool IsActiveAt(DateTimeOffset now) =>
        RevokedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now, string? ipAddress)
    {
        LastUsedAt = now;
        LastUsedIp = ipAddress;
    }

    public void Revoke(
        DateTimeOffset now,
        string reason,
        Guid? replacedBySessionId = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevocationReason = reason;
        ReplacedBySessionId = replacedBySessionId;
    }
}
