using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Email).HasMaxLength(254).IsRequired();
        builder.Property(entity => entity.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(entity => entity.PasswordHash).HasMaxLength(512).IsRequired();
        builder.HasIndex(entity => entity.NormalizedEmail).IsUnique();
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RevocationReason).HasMaxLength(200);
        builder.Property(entity => entity.CreatedByIp).HasMaxLength(64);
        builder.Property(entity => entity.LastUsedIp).HasMaxLength(64);
        builder.Property(entity => entity.UserAgent).HasMaxLength(512);
        builder.HasIndex(entity => entity.RefreshTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.UserAccountId, entity.RevokedAt });

        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserSession>()
            .WithMany()
            .HasForeignKey(entity => entity.ReplacedBySessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Slug).HasMaxLength(100).IsRequired();
        builder
            .Property(entity => entity.OrganizationType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.TimeZone).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder
            .Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(entity => entity.Slug).IsUnique();
    }
}

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.LastName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ContactEmail).HasMaxLength(254);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(40);
        builder.Property(entity => entity.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.TimeZone).HasMaxLength(100).IsRequired();
        builder
            .HasIndex(entity => new
            {
                entity.OrganizationId,
                entity.LinkedUserAccountId
            })
            .IsUnique()
            .HasFilter("linked_user_account_id IS NOT NULL AND archived_at IS NULL");

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.LinkedUserAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class OrganizationMembershipConfiguration
    : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("organization_memberships");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder
            .Property(entity => entity.BaseRole)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder
            .Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder
            .HasIndex(entity => new { entity.OrganizationId, entity.UserAccountId })
            .IsUnique()
            .HasFilter("status = 'Active'");

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PermissionGrantConfiguration
    : IEntityTypeConfiguration<PermissionGrant>
{
    public void Configure(EntityTypeBuilder<PermissionGrant> builder)
    {
        builder.ToTable("permission_grants", table =>
        {
            table.HasCheckConstraint(
                "ck_permission_grants_subject",
                "(user_account_id IS NOT NULL AND organization_membership_id IS NULL) OR "
                + "(user_account_id IS NULL AND organization_membership_id IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_permission_grants_scope",
                "(scope = 'Organization' AND event_id IS NULL) OR "
                + "(scope = 'Event' AND event_id IS NOT NULL)");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Permission).HasMaxLength(120).IsRequired();
        builder
            .Property(entity => entity.Effect)
            .HasConversion<string>()
            .HasMaxLength(16);
        builder
            .Property(entity => entity.Scope)
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.UserAccountId,
            entity.Permission,
            entity.EventId
        });
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.OrganizationMembershipId,
            entity.Permission,
            entity.EventId
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<OrganizationMembership>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.OrganizationMembershipId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
