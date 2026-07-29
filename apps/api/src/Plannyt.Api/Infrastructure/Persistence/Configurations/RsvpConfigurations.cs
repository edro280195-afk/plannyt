using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class EventRsvpSettingsConfiguration
    : IEntityTypeConfiguration<EventRsvpSettings>
{
    public void Configure(EntityTypeBuilder<EventRsvpSettings> builder)
    {
        builder.ToTable("event_rsvp_settings");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.TimeZone).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.ConfirmationTitle).HasMaxLength(2000);
        builder.Property(entity => entity.ConfirmationMessage).HasMaxLength(2000);
        builder.Property(entity => entity.DeclineMessage).HasMaxLength(2000);
        builder.Property(entity => entity.ClosedMessage).HasMaxLength(2000);
        builder.Property(entity => entity.PrivacyNotice).HasMaxLength(2000);
        builder.Property(entity => entity.SensitiveDataConsentText).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId })
            .IsUnique();
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RsvpFormConfiguration : IEntityTypeConfiguration<RsvpForm>
{
    public void Configure(EntityTypeBuilder<RsvpForm> builder)
    {
        builder.ToTable("rsvp_forms");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.EventId })
            .IsUnique();
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RsvpFormVersionConfiguration
    : IEntityTypeConfiguration<RsvpFormVersion>
{
    public void Configure(EntityTypeBuilder<RsvpFormVersion> builder)
    {
        builder.ToTable("rsvp_form_versions");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.RsvpFormId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.SettingsSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.QuestionsSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.MenuSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.TransportSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.AccommodationSnapshot).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.RsvpFormId,
            entity.VersionNumber
        })
            .IsUnique();
        builder.HasOne<RsvpForm>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.RsvpFormId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CurrentGuestRsvpConfiguration
    : IEntityTypeConfiguration<CurrentGuestRsvp>
{
    public void Configure(EntityTypeBuilder<CurrentGuestRsvp> builder)
    {
        builder.ToTable("current_guest_rsvps");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.AttendanceStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.CurrentDisplayName).HasMaxLength(200);
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
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.LastSubmissionId
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

internal sealed class RsvpSubmissionConfiguration
    : IEntityTypeConfiguration<RsvpSubmission>
{
    public void Configure(EntityTypeBuilder<RsvpSubmission> builder)
    {
        builder.ToTable("rsvp_submissions");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Source).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.OverallStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.ContactNameSnapshot).HasMaxLength(254);
        builder.Property(entity => entity.ContactEmailSnapshot).HasMaxLength(254);
        builder.Property(entity => entity.ContactPhoneSnapshot).HasMaxLength(254);
        builder.Property(entity => entity.IpAddress).HasMaxLength(45);
        builder.Property(entity => entity.UserAgentCategory).HasMaxLength(50);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ConsentSnapshot).HasColumnType("jsonb");
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId,
            entity.IdempotencyKey
        })
            .IsUnique()
            .HasDatabaseName("ux_rsvp_submissions_idempotency");
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId,
            entity.RevisionNumber
        })
            .IsUnique()
            .HasDatabaseName("ux_rsvp_submissions_revision");
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
        builder.HasOne<RsvpFormVersion>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.RsvpFormVersionId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GuestAccessLink>()
            .WithMany()
            .HasForeignKey(entity => entity.GuestAccessLinkId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.PreviousSubmissionId
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

internal sealed class RsvpSubmissionGuestConfiguration
    : IEntityTypeConfiguration<RsvpSubmissionGuest>
{
    public void Configure(EntityTypeBuilder<RsvpSubmissionGuest> builder)
    {
        builder.ToTable("rsvp_submission_guests");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new
        {
            entity.RsvpSubmissionId,
            entity.ResponseGuestId
        });
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.AgeCategory).HasMaxLength(24);
        builder.Property(entity => entity.AttendanceStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.MenuSelectionsSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.TransportSelectionSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.AccommodationSelectionSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.DietarySnapshot).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new
        {
            entity.RsvpSubmissionId,
            entity.EventGuestId
        })
            .IsUnique()
            .HasFilter("event_guest_id IS NOT NULL");
        builder.HasIndex(entity => new
        {
            entity.RsvpSubmissionId,
            entity.CompanionSlotNumber
        })
            .IsUnique()
            .HasFilter("companion_slot_number IS NOT NULL");
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => entity.RsvpSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RsvpSubmissionAnswerConfiguration
    : IEntityTypeConfiguration<RsvpSubmissionAnswer>
{
    public void Configure(EntityTypeBuilder<RsvpSubmissionAnswer> builder)
    {
        builder.ToTable("rsvp_submission_answers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.QuestionId).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.AnswerValue).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.DisplayValueSnapshot).HasMaxLength(1000);
        builder.Property(entity => entity.QuestionLabelSnapshot)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.QuestionTypeSnapshot)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.OptionLabelsSnapshot)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(entity => entity.GuestDisplayNameSnapshot)
            .HasMaxLength(200);
        builder.HasIndex(entity => new
        {
            entity.RsvpSubmissionId,
            entity.QuestionId,
            entity.GuestId
        })
            .IsUnique()
            .HasFilter("guest_id IS NOT NULL");
        builder.HasIndex(entity => new
        {
            entity.RsvpSubmissionId,
            entity.QuestionId
        })
            .IsUnique()
            .HasFilter("guest_id IS NULL");
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => entity.RsvpSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RsvpSubmissionGuest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.RsvpSubmissionId,
                ResponseGuestId = entity.GuestId
            })
            .HasPrincipalKey(entity => new
            {
                entity.RsvpSubmissionId,
                entity.ResponseGuestId
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventMenuConfiguration : IEntityTypeConfiguration<EventMenu>
{
    public void Configure(EntityTypeBuilder<EventMenu> builder)
    {
        builder.ToTable("event_menus");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.MenuCategory).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.SortOrder
        });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventMenuOptionConfiguration
    : IEntityTypeConfiguration<EventMenuOption>
{
    public void Configure(EntityTypeBuilder<EventMenuOption> builder)
    {
        builder.ToTable("event_menu_options");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventMenuId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.DietaryTags).HasMaxLength(500);
        builder.HasOne<EventMenu>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventMenuId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GuestDietaryAndAccessibilityConfiguration
    : IEntityTypeConfiguration<GuestDietaryAndAccessibility>
{
    public void Configure(EntityTypeBuilder<GuestDietaryAndAccessibility> builder)
    {
        builder.ToTable("guest_dietary_accessibility");
        builder.HasKey(entity => entity.EventGuestId);
        builder.Property(entity => entity.Allergies).HasMaxLength(1000);
        builder.Property(entity => entity.DietaryRestrictions).HasMaxLength(1000);
        builder.Property(entity => entity.AccessibilityRequirements).HasMaxLength(1000);
        builder.Property(entity => entity.AdditionalNotes).HasMaxLength(2000);
        builder.HasOne<EventGuest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventGuestId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.LastSubmissionId
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

internal sealed class EventTransportOptionConfiguration
    : IEntityTypeConfiguration<EventTransportOption>
{
    public void Configure(EntityTypeBuilder<EventTransportOption> builder)
    {
        builder.ToTable("event_transport_options");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Direction).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.PickupPoint).HasMaxLength(500);
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GuestTransportSelectionConfiguration
    : IEntityTypeConfiguration<GuestTransportSelection>
{
    public void Configure(EntityTypeBuilder<GuestTransportSelection> builder)
    {
        builder.ToTable("guest_transport_selections");
        builder.HasKey(entity => new { entity.EventGuestId, entity.EventTransportOptionId });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.EventTransportOptionId,
            entity.WaitlistSequence
        })
            .IsUnique()
            .HasFilter("waitlist_sequence IS NOT NULL");
        builder.HasOne<EventGuest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventGuestId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventTransportOption>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventTransportOptionId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.LastSubmissionId
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

internal sealed class GuestTransportSelectionHistoryConfiguration
    : IEntityTypeConfiguration<GuestTransportSelectionHistory>
{
    public void Configure(
        EntityTypeBuilder<GuestTransportSelectionHistory> builder)
    {
        builder.ToTable("guest_transport_selection_history");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.EventTransportOptionId,
            entity.OccurredAt
        });
        builder.HasOne<EventGuest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventGuestId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventTransportOption>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventTransportOptionId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.SubmissionId
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

internal sealed class EventAccommodationOptionConfiguration
    : IEntityTypeConfiguration<EventAccommodationOption>
{
    public void Configure(EntityTypeBuilder<EventAccommodationOption> builder)
    {
        builder.ToTable("event_accommodation_options");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.Address).HasMaxLength(500);
        builder.Property(entity => entity.BookingUrl).HasMaxLength(500);
        builder.Property(entity => entity.BookingCode).HasMaxLength(100);
        builder.Property(entity => entity.ContactInformation).HasMaxLength(500);
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GuestAccommodationSelectionConfiguration
    : IEntityTypeConfiguration<GuestAccommodationSelection>
{
    public void Configure(EntityTypeBuilder<GuestAccommodationSelection> builder)
    {
        builder.ToTable("guest_accommodation_selections");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.ReservationName).HasMaxLength(200);
        builder.Property(entity => entity.ConfirmationReference).HasMaxLength(200);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.EventGuestId
        })
            .IsUnique()
            .HasFilter("event_guest_id IS NOT NULL");
        builder.HasOne<EventGuest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventGuestId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventAccommodationOption>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.EventAccommodationOptionId
            })
            .HasPrincipalKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.Id
            })
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
        builder.HasOne<RsvpSubmission>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.LastSubmissionId
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

internal sealed class RsvpGroupExceptionConfiguration
    : IEntityTypeConfiguration<RsvpGroupException>
{
    public void Configure(EntityTypeBuilder<RsvpGroupException> builder)
    {
        builder.ToTable("rsvp_group_exceptions");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.EventId, entity.Id });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.InvitationGroupId,
            entity.Status
        })
            .IsUnique()
            .HasFilter("status = 'Active'");
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
    }
}

internal sealed class ReminderTemplateConfiguration
    : IEntityTypeConfiguration<ReminderTemplate>
{
    public void Configure(EntityTypeBuilder<ReminderTemplate> builder)
    {
        builder.ToTable("reminder_templates");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.SegmentType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.MessageTemplate).HasMaxLength(4000).IsRequired();
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventReminderLogConfiguration
    : IEntityTypeConfiguration<EventReminderLog>
{
    public void Configure(EntityTypeBuilder<EventReminderLog> builder)
    {
        builder.ToTable("event_reminder_logs");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Note).HasMaxLength(1000);
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
        builder.HasOne<ReminderTemplate>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReminderTemplateId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
