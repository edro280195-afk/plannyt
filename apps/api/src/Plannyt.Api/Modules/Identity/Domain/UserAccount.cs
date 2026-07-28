namespace Plannyt.Api.Modules.Identity.Domain;

public sealed class UserAccount
{
    private UserAccount()
    {
    }

    private UserAccount(
        Guid id,
        string email,
        string normalizedEmail,
        string passwordHash,
        DateTimeOffset now)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        IsActive = true;
        SecurityVersion = 1;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public bool IsActive { get; private set; }

    public int SecurityVersion { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserAccount Create(
        string email,
        string normalizedEmail,
        string passwordHash,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), email, normalizedEmail, passwordHash, now);

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        SecurityVersion++;
        UpdatedAt = now;
    }

    public void IncrementSecurityVersion(DateTimeOffset now)
    {
        SecurityVersion++;
        UpdatedAt = now;
    }
}
