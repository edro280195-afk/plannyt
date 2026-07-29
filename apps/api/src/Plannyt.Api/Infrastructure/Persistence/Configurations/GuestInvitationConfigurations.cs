using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class InvitationGroupConfiguration
    : IEntityTypeConfiguration<InvitationGroup>
{
    public void Configure(EntityTypeBuilder<InvitationGroup> builder)
    {
        builder.ToTable("invitation_groups", table =>
        {
            table.HasCheckConstraint(
                "ck_invitation_groups_capacity",
                "allowed_guest_count >= 1 AND max_unnamed_companions >= 0 "
                + "AND max_unnamed_companions <= allowed_guest_count");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.GroupType).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ContactName).HasMaxLength(160);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.ContactEmail).HasMaxLength(254);
        builder.Property(entity => entity.Source).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.InternalNotes).HasMaxLength(4000);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId, entity.DisplayName });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventGuestConfiguration : IEntityTypeConfiguration<EventGuest>
{
    public void Configure(EntityTypeBuilder<EventGuest> builder)
    {
        builder.ToTable("event_guests");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.LastName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(254);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.Property(entity => entity.GuestType).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.AgeCategory).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.InternalNotes).HasMaxLength(4000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId
        });
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId,
            entity.IsPrimaryContact
        })
            .IsUnique()
            .HasFilter("archived_at IS NULL AND is_primary_contact = TRUE");
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvitationGroup>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationGroupId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GuestTagConfiguration : IEntityTypeConfiguration<GuestTag>
{
    public void Configure(EntityTypeBuilder<GuestTag> builder)
    {
        builder.ToTable("guest_tags");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(60).IsRequired();
        builder.Property(entity => entity.ColorToken).HasMaxLength(24).IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId, entity.Name })
            .IsUnique()
            .HasFilter("archived_at IS NULL");
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvitationGroupTagConfiguration
    : IEntityTypeConfiguration<InvitationGroupTag>
{
    public void Configure(EntityTypeBuilder<InvitationGroupTag> builder)
    {
        builder.ToTable("invitation_group_tags");
        builder.HasKey(entity => new { entity.InvitationGroupId, entity.GuestTagId });
        builder.HasOne<InvitationGroup>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationGroupId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<GuestTag>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.GuestTagId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GuestImportBatchConfiguration
    : IEntityTypeConfiguration<GuestImportBatch>
{
    public void Configure(EntityTypeBuilder<GuestImportBatch> builder)
    {
        builder.ToTable("guest_import_batches");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.CsvContent).IsRequired();
        builder.Property(entity => entity.MappingJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.AnalysisJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.ResultJson).HasColumnType("jsonb");
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId, entity.CreatedAt });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventGuestExperienceConfiguration
    : IEntityTypeConfiguration<EventGuestExperience>
{
    public void Configure(EntityTypeBuilder<EventGuestExperience> builder)
    {
        builder.ToTable("event_guest_experiences");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId }).IsUnique();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Language).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.PublicTitle).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.CelebrantDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.WelcomeMessage).HasMaxLength(1000);
        builder.Property(entity => entity.ClosingMessage).HasMaxLength(1000);
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvitationDesignConfiguration
    : IEntityTypeConfiguration<InvitationDesign>
{
    public void Configure(EntityTypeBuilder<InvitationDesign> builder)
    {
        builder.ToTable("invitation_designs");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.DraftThemeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.DraftContentJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId, entity.UpdatedAt });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvitationDesignVersionConfiguration
    : IEntityTypeConfiguration<InvitationDesignVersion>
{
    public void Configure(EntityTypeBuilder<InvitationDesignVersion> builder)
    {
        builder.ToTable("invitation_design_versions");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationDesignId,
            entity.Id
        });
        builder.Property(entity => entity.ThemeSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.ContentSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationDesignId,
            entity.VersionNumber
        })
            .IsUnique();
        builder.HasOne<InvitationDesign>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationDesignId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvitationDesignCommentConfiguration
    : IEntityTypeConfiguration<InvitationDesignComment>
{
    public void Configure(EntityTypeBuilder<InvitationDesignComment> builder)
    {
        builder.ToTable("invitation_design_comments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Decision).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationDesignId,
            entity.CreatedAt
        });
        builder.HasOne<InvitationDesignVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationDesignId,
                entity.InvitationDesignVersionId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationDesignId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvitationTemplateConfiguration
    : IEntityTypeConfiguration<InvitationTemplate>
{
    public void Configure(EntityTypeBuilder<InvitationTemplate> builder)
    {
        builder.ToTable("invitation_templates");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(240).IsRequired();
        builder.Property(entity => entity.ThemeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.ContentJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Name });
    }
}

internal sealed class GuestAccessLinkConfiguration
    : IEntityTypeConfiguration<GuestAccessLink>
{
    public void Configure(EntityTypeBuilder<GuestAccessLink> builder)
    {
        builder.ToTable("guest_access_links");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.DerivationKeyId).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId,
            entity.Status
        })
            .IsUnique()
            .HasFilter("status = 'Active'");
        builder.HasOne<InvitationGroup>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.InvitationGroupId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
