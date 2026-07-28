using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class EventAccessConfiguration : IEntityTypeConfiguration<EventAccess>
{
    public void Configure(EntityTypeBuilder<EventAccess> builder)
    {
        builder.ToTable("event_accesses", table =>
        {
            table.HasCheckConstraint(
                "ck_event_accesses_dates",
                "expires_at IS NULL OR expires_at > starts_at");
            table.HasCheckConstraint(
                "ck_event_accesses_revoked_at",
                "(status = 'Revoked' AND revoked_at IS NOT NULL) OR "
                + "(status <> 'Revoked' AND revoked_at IS NULL)");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder
            .Property(entity => entity.BaseRole)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder
            .Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder
            .HasIndex(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.UserAccountId
            })
            .IsUnique()
            .HasFilter("status <> 'Revoked' AND revoked_at IS NULL");

        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccessInvitationConfiguration
    : IEntityTypeConfiguration<AccessInvitation>
{
    public void Configure(EntityTypeBuilder<AccessInvitation> builder)
    {
        builder.ToTable("access_invitations", table =>
        {
            table.HasCheckConstraint(
                "ck_access_invitations_type",
                "(invitation_type = 'OrganizationMembership' "
                + "AND event_id IS NULL "
                + "AND intended_organization_role IS NOT NULL "
                + "AND intended_event_role IS NULL) OR "
                + "(invitation_type = 'EventAccess' "
                + "AND event_id IS NOT NULL "
                + "AND intended_organization_role IS NULL "
                + "AND intended_event_role IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_access_invitations_completion",
                "accepted_at IS NULL OR revoked_at IS NULL");
            table.HasCheckConstraint(
                "ck_access_invitations_expiry",
                "expires_at > created_at");
        });
        builder.HasKey(entity => entity.Id);
        builder
            .Property(entity => entity.InvitationType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder
            .Property(entity => entity.IntendedOrganizationRole)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder
            .Property(entity => entity.IntendedEventRole)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.Property(entity => entity.TargetEmail).HasMaxLength(254).IsRequired();
        builder.Property(entity => entity.NormalizedTargetEmail).HasMaxLength(254).IsRequired();
        builder.Property(entity => entity.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.NormalizedTargetEmail,
            entity.ExpiresAt
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BasicDocumentConfiguration : IEntityTypeConfiguration<BasicDocument>
{
    public void Configure(EntityTypeBuilder<BasicDocument> builder)
    {
        builder.ToTable("basic_documents", table =>
        {
            table.HasCheckConstraint(
                "ck_basic_documents_size",
                "size_bytes > 0 AND size_bytes <= 10485760");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.DocumentType).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.StorageProvider).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(100).IsRequired();
        builder
            .Property(entity => entity.Visibility)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => entity.StorageKey).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.Visibility,
            entity.DeletedAt
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Action).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Metadata).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.OccurredAt
        });
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EntityType,
            entity.EntityId
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
