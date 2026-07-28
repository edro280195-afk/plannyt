using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients", table =>
        {
            table.HasCheckConstraint(
                "ck_clients_type",
                "(client_type = 'Person' AND person_id IS NOT NULL AND company_name IS NULL) OR "
                + "(client_type = 'Company' AND person_id IS NULL AND company_name IS NOT NULL)");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder
            .Property(entity => entity.ClientType)
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(entity => entity.CompanyName).HasMaxLength(200);
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder
            .Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.Source).HasMaxLength(100);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.DisplayName });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ClientContactConfiguration : IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> builder)
    {
        builder.ToTable("client_contacts");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ContactRole).HasMaxLength(80).IsRequired();
        builder
            .HasIndex(entity => new { entity.OrganizationId, entity.ClientId })
            .HasFilter("is_primary")
            .IsUnique();

        builder
            .HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", table =>
        {
            table.HasCheckConstraint(
                "ck_events_dates",
                "end_date_time IS NULL OR end_date_time >= start_date_time");
            table.HasCheckConstraint(
                "ck_events_guest_count",
                "estimated_guest_count IS NULL OR estimated_guest_count >= 0");
            table.HasCheckConstraint(
                "ck_events_archived_at",
                "(status = 'Archived' AND archived_at IS NOT NULL) OR "
                + "(status <> 'Archived' AND archived_at IS NULL)");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.EventType).HasMaxLength(80).IsRequired();
        builder
            .Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder
            .Property(entity => entity.StatusBeforeSuspension)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.TimeZone).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.City).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(entity => entity.SharedDescription).HasMaxLength(2000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.StartDateTime
        });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventStatusHistoryConfiguration
    : IEntityTypeConfiguration<EventStatusHistory>
{
    public void Configure(EntityTypeBuilder<EventStatusHistory> builder)
    {
        builder.ToTable("event_status_history");
        builder.HasKey(entity => entity.Id);
        builder
            .Property(entity => entity.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder
            .Property(entity => entity.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.ChangedAt
        });

        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventClientConfiguration : IEntityTypeConfiguration<EventClient>
{
    public void Configure(EntityTypeBuilder<EventClient> builder)
    {
        builder.ToTable("event_clients");
        builder.HasKey(entity => entity.Id);
        builder
            .Property(entity => entity.RelationshipType)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder
            .HasIndex(entity => new { entity.OrganizationId, entity.EventId })
            .HasFilter("is_primary")
            .IsUnique();
        builder
            .HasIndex(entity => new
            {
                entity.OrganizationId,
                entity.EventId,
                entity.ClientId,
                entity.RelationshipType
            })
            .IsUnique();

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
    }
}

internal sealed class EventParticipantConfiguration
    : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.ToTable("event_participants");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ParticipantType).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.SharedDescription).HasMaxLength(1000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.DisplayOrder
        });

        builder
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
