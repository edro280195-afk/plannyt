using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProspectConfiguration : IEntityTypeConfiguration<Prospect>
{
    public void Configure(EntityTypeBuilder<Prospect> builder)
    {
        builder.ToTable("prospects", table =>
        {
            table.HasCheckConstraint(
                "ck_prospects_guest_count",
                "estimated_guest_count IS NULL OR estimated_guest_count >= 0");
            table.HasCheckConstraint(
                "ck_prospects_budget",
                "estimated_budget IS NULL OR estimated_budget >= 0");
            table.HasCheckConstraint(
                "ck_prospects_lost_reason",
                "(status = 'Lost' AND lost_reason IS NOT NULL) OR status <> 'Lost'");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.FirstName).HasMaxLength(100);
        builder.Property(entity => entity.LastName).HasMaxLength(100);
        builder.Property(entity => entity.CompanyName).HasMaxLength(200);
        builder.Property(entity => entity.Email).HasMaxLength(254);
        builder.Property(entity => entity.Phone).HasMaxLength(40);
        builder.Property(entity => entity.Source).HasMaxLength(100);
        builder.Property(entity => entity.EventTypeInterest).HasMaxLength(80);
        builder.Property(entity => entity.EstimatedBudget).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.City).HasMaxLength(120);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.Property(entity => entity.LostReason).HasMaxLength(500);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.AssignedUserId });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Email });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Phone });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ConvertedClientId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProspectStatusHistoryConfiguration
    : IEntityTypeConfiguration<ProspectStatusHistory>
{
    public void Configure(EntityTypeBuilder<ProspectStatusHistory> builder)
    {
        builder.ToTable("prospect_status_history");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProspectId,
            entity.ChangedAt
        });
        builder.HasOne<Prospect>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProspectId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProspectActivityConfiguration
    : IEntityTypeConfiguration<ProspectActivity>
{
    public void Configure(EntityTypeBuilder<ProspectActivity> builder)
    {
        builder.ToTable("prospect_activities");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ActivityType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Subject).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(4000);
        builder.Property(entity => entity.Visibility)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProspectId,
            entity.ScheduledAt
        });
        builder.HasOne<Prospect>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProspectId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServiceCatalogItemConfiguration
    : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.ToTable("service_catalog_items", table =>
        {
            table.HasCheckConstraint("ck_catalog_base_price", "base_price >= 0");
            table.HasCheckConstraint("ck_catalog_sort_order", "sort_order >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PricingType)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.BasePrice).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.TaxBehavior)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.IsActive,
            entity.SortOrder
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages", table =>
            table.HasCheckConstraint("ck_packages_base_price", "base_price >= 0"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.BasePrice).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IsActive });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.ToTable("package_items", table =>
        {
            table.HasCheckConstraint("ck_package_items_quantity", "quantity > 0");
            table.HasCheckConstraint(
                "ck_package_items_price",
                "included_price IS NULL OR included_price >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Quantity).HasPrecision(12, 2);
        builder.Property(entity => entity.IncludedPrice).HasPrecision(18, 2);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.PackageId,
            entity.ServiceCatalogItemId
        }).IsUnique();
        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PackageId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ServiceCatalogItem>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ServiceCatalogItemId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons", table =>
        {
            table.HasCheckConstraint("ck_coupons_value", "discount_value >= 0");
            table.HasCheckConstraint("ck_coupons_dates", "ends_at >= starts_at");
            table.HasCheckConstraint(
                "ck_coupons_uses",
                "current_uses >= 0 AND (maximum_uses IS NULL OR maximum_uses > 0)");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.DiscountValue).HasPrecision(18, 2);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Code }).IsUnique();
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("proposals", table =>
        {
            table.HasCheckConstraint(
                "ck_proposals_target",
                "prospect_id IS NOT NULL OR client_id IS NOT NULL");
            table.HasCheckConstraint(
                "ck_proposals_discount",
                "general_discount_value >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.ProposalNumber).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.SharedIntroduction).HasMaxLength(4000);
        builder.Property(entity => entity.SharedTerms).HasMaxLength(8000);
        builder.Property(entity => entity.InternalNotes).HasMaxLength(4000);
        builder.Property(entity => entity.GeneralDiscountType)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.GeneralDiscountValue).HasPrecision(18, 2);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalNumber
        }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status });
        builder.HasOne<Prospect>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProspectId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Coupon>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CouponId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.AcceptedVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalDraftLineConfiguration
    : IEntityTypeConfiguration<ProposalDraftLine>
{
    public void Configure(EntityTypeBuilder<ProposalDraftLine> builder)
    {
        builder.ToTable("proposal_draft_lines", table =>
        {
            table.HasCheckConstraint("ck_draft_lines_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_draft_lines_price", "unit_price >= 0");
            table.HasCheckConstraint(
                "ck_draft_lines_discount",
                "discount_value >= 0");
            table.HasCheckConstraint(
                "ck_draft_lines_tax",
                "tax_rate >= 0 AND tax_rate <= 100");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(12, 2);
        builder.Property(entity => entity.UnitPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.DiscountValue).HasPrecision(18, 2);
        builder.Property(entity => entity.TaxRate).HasPrecision(7, 4);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalId,
            entity.SortOrder
        });
        builder.HasOne<Proposal>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProposalId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ServiceCatalogItem>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ServiceCatalogItemId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PackageId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalVersionConfiguration
    : IEntityTypeConfiguration<ProposalVersion>
{
    public void Configure(EntityTypeBuilder<ProposalVersion> builder)
    {
        builder.ToTable("proposal_versions", table =>
        {
            table.HasCheckConstraint(
                "ck_proposal_versions_totals",
                "subtotal >= 0 AND discount_total >= 0 AND tax_total >= 0 "
                + "AND grand_total >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Subtotal).HasPrecision(18, 2);
        builder.Property(entity => entity.DiscountTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.TaxTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.GrandTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.SharedIntroduction).HasMaxLength(4000);
        builder.Property(entity => entity.SharedTerms).HasMaxLength(8000);
        builder.Property(entity => entity.GeneralDiscountType)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.GeneralDiscountValue).HasPrecision(18, 2);
        builder.Property(entity => entity.GeneralDiscountTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.CouponCode).HasMaxLength(40);
        builder.Property(entity => entity.CouponDiscountTotal).HasPrecision(18, 2);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalId,
            entity.VersionNumber
        }).IsUnique();
        builder.HasOne<Proposal>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProposalId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Coupon>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CouponId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalLineConfiguration
    : IEntityTypeConfiguration<ProposalLine>
{
    public void Configure(EntityTypeBuilder<ProposalLine> builder)
    {
        builder.ToTable("proposal_lines");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(12, 2);
        builder.Property(entity => entity.UnitPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.DiscountType).HasMaxLength(24).IsRequired();
        builder.Property(entity => entity.DiscountValue).HasPrecision(18, 2);
        builder.Property(entity => entity.TaxRate).HasPrecision(7, 4);
        builder.Property(entity => entity.LineSubtotal).HasPrecision(18, 2);
        builder.Property(entity => entity.LineDiscount).HasPrecision(18, 2);
        builder.Property(entity => entity.LineTax).HasPrecision(18, 2);
        builder.Property(entity => entity.LineTotal).HasPrecision(18, 2);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalVersionId,
            entity.SortOrder
        });
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ProposalVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalCommentConfiguration
    : IEntityTypeConfiguration<ProposalComment>
{
    public void Configure(EntityTypeBuilder<ProposalComment> builder)
    {
        builder.ToTable("proposal_comments");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.AuthorDisplayName)
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(entity => entity.Content).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Visibility)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalVersionId,
            entity.CreatedAt
        });
        builder.HasOne<Proposal>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProposalId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ProposalVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalLine>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ProposalLineId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalComment>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ParentCommentId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProposalShareLinkConfiguration
    : IEntityTypeConfiguration<ProposalShareLink>
{
    public void Configure(EntityTypeBuilder<ProposalShareLink> builder)
    {
        builder.ToTable("proposal_share_links");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ProposalId,
            entity.RevokedAt
        });
        builder.HasOne<Proposal>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ProposalId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ProposalVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
