using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Payments.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Infrastructure.Persistence.Configurations;

internal sealed class ContractTemplateConfiguration
    : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("contract_templates");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.Content).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.ContentFormat)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.IsActive,
            entity.IsDefault
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrganizationContractingPolicyConfiguration
    : IEntityTypeConfiguration<OrganizationContractingPolicy>
{
    public void Configure(
        EntityTypeBuilder<OrganizationContractingPolicy> builder)
    {
        builder.ToTable("organization_contracting_policies", table =>
            table.HasCheckConstraint(
                "ck_contracting_policy_deposit",
                "deposit_requirement_value >= 0 "
                + "AND (deposit_requirement_type <> 'PercentageOfContract' "
                + "OR deposit_requirement_value <= 100) "
                + "AND (deposit_requirement_type <> 'None' "
                + "OR deposit_requirement_value = 0)"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.HasIndex(entity => entity.OrganizationId).IsUnique();
        builder.Property(entity => entity.DepositRequirementType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.DepositRequirementValue)
            .HasPrecision(18, 2);
        builder.Property(entity => entity.ConfirmationMode)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts", table =>
        {
            table.HasCheckConstraint(
                "ck_contracts_source",
                "(source_type = 'GeneratedFromProposal' "
                + "AND accepted_proposal_id IS NOT NULL "
                + "AND accepted_proposal_version_id IS NOT NULL) "
                + "OR (source_type <> 'GeneratedFromProposal' "
                + "AND accepted_proposal_id IS NULL "
                + "AND accepted_proposal_version_id IS NULL)");
            table.HasCheckConstraint(
                "ck_contracts_total",
                "contract_grand_total >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.ContractNumber)
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.ContractGrandTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractNumber
        }).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.Status
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Proposal>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.AcceptedProposalId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.AcceptedProposalVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractRequirementSnapshotConfiguration
    : IEntityTypeConfiguration<ContractingRequirementSnapshot>
{
    public void Configure(
        EntityTypeBuilder<ContractingRequirementSnapshot> builder)
    {
        builder.ToTable("contracting_requirement_snapshots", table =>
            table.HasCheckConstraint(
                "ck_requirement_snapshot_amounts",
                "deposit_requirement_value >= 0 "
                + "AND required_deposit_amount >= 0"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.DepositRequirementType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.DepositRequirementValue)
            .HasPrecision(18, 2);
        builder.Property(entity => entity.RequiredDepositAmount)
            .HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.ConfirmationMode)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractId
        }).IsUnique();
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractVersionConfiguration
    : IEntityTypeConfiguration<ContractVersion>
{
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.ToTable("contract_versions", table =>
        {
            table.HasCheckConstraint(
                "ck_contract_versions_document",
                "(published_at IS NULL "
                + "AND document_storage_key IS NULL "
                + "AND document_sha256 IS NULL) OR "
                + "(published_at IS NOT NULL "
                + "AND document_storage_key IS NOT NULL "
                + "AND document_size_bytes > 0 "
                + "AND length(document_sha256) = 64)");
            table.HasCheckConstraint(
                "ck_contract_versions_validity",
                "valid_until IS NULL OR valid_until > created_at");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.RenderedContent)
            .HasColumnType("text")
            .IsRequired();
        builder.Property(entity => entity.DocumentStorageKey).HasMaxLength(500);
        builder.Property(entity => entity.DocumentFileName).HasMaxLength(255);
        builder.Property(entity => entity.DocumentMimeType).HasMaxLength(100);
        builder.Property(entity => entity.DocumentSha256).HasMaxLength(64);
        builder.Property(entity => entity.ConsentText)
            .HasMaxLength(4000)
            .IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractId,
            entity.VersionNumber
        }).IsUnique();
        builder.HasIndex(entity => entity.DocumentStorageKey).IsUnique();
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractTemplate>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.TemplateId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProposalVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.SourceProposalVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractPartyConfiguration
    : IEntityTypeConfiguration<ContractParty>
{
    public void Configure(EntityTypeBuilder<ContractParty> builder)
    {
        builder.ToTable("contract_parties", table =>
            table.HasCheckConstraint(
                "ck_contract_parties_type",
                "(party_type = 'Client' AND client_id IS NOT NULL) "
                + "OR (party_type = 'PlannerOrganization' "
                + "AND organization_party_id IS NOT NULL) "
                + "OR party_type = 'Other'"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.PartyType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.LegalName).HasMaxLength(250);
        builder.Property(entity => entity.TaxId).HasMaxLength(40);
        builder.Property(entity => entity.Address).HasMaxLength(1000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractId,
            entity.SortOrder
        });
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationPartyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractSignerConfiguration
    : IEntityTypeConfiguration<ContractSigner>
{
    public void Configure(EntityTypeBuilder<ContractSigner> builder)
    {
        builder.ToTable("contract_signers");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(254).IsRequired();
        builder.Property(entity => entity.SignerRole).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractId,
            entity.SigningOrder
        });
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractParty>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractPartyId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PersonId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SignatureRequestConfiguration
    : IEntityTypeConfiguration<SignatureRequest>
{
    public void Configure(EntityTypeBuilder<SignatureRequest> builder)
    {
        builder.ToTable("signature_requests", table =>
            table.HasCheckConstraint(
                "ck_signature_requests_expiry",
                "expires_at > created_at"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractSignerId,
            entity.RevokedAt,
            entity.SignedAt
        });
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractSigner>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractSignerId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SignatureEvidenceConfiguration
    : IEntityTypeConfiguration<SignatureEvidence>
{
    public void Configure(EntityTypeBuilder<SignatureEvidence> builder)
    {
        builder.ToTable("signature_evidence", table =>
            table.HasCheckConstraint(
                "ck_signature_evidence_sha256",
                "length(document_sha256) = 64"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.SigningMethod)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.Property(entity => entity.DeclaredSignerName)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.DeclaredSignerEmail)
            .HasMaxLength(254)
            .IsRequired();
        builder.Property(entity => entity.SignatureImageStorageKey).HasMaxLength(500);
        builder.Property(entity => entity.DocumentSha256)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.ConsentText)
            .HasMaxLength(4000)
            .IsRequired();
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.UserAgent).HasMaxLength(512);
        builder.Property(entity => entity.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.EvidenceJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractVersionId,
            entity.ContractSignerId
        }).IsUnique();
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractSigner>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractSignerId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SignatureRequest>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.SignatureRequestId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractFinalDocumentConfiguration
    : IEntityTypeConfiguration<ContractFinalDocument>
{
    public void Configure(EntityTypeBuilder<ContractFinalDocument> builder)
    {
        builder.ToTable("contract_final_documents", table =>
            table.HasCheckConstraint(
                "ck_contract_final_document",
                "size_bytes > 0 AND length(sha256) = 64"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.Sha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => entity.StorageKey).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.ContractId
        }).IsUnique();
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.ContractVersionId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentPlanConfiguration
    : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> builder)
    {
        builder.ToTable("payment_plans", table =>
            table.HasCheckConstraint(
                "ck_payment_plans_amount",
                "total_amount >= 0 "
                + "AND (activated_total_amount IS NULL "
                + "OR activated_total_amount >= 0)"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.TotalAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.ActivatedTotalAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.Status
        });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContractId })
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

internal sealed class PaymentInstallmentConfiguration
    : IEntityTypeConfiguration<PaymentInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentInstallment> builder)
    {
        builder.ToTable("payment_installments", table =>
            table.HasCheckConstraint(
                "ck_payment_installments_values",
                "sequence_number > 0 AND amount >= 0"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Description).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.InstallmentType)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.PaymentPlanId,
            entity.SequenceNumber
        }).IsUnique();
        builder.HasOne<PaymentPlan>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.PaymentPlanId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentRecordConfiguration
    : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payment_records", table =>
            table.HasCheckConstraint(
                "ck_payment_records_amount",
                "amount > 0"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.Method)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entity => entity.Reference).HasMaxLength(200);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entity => entity.NotesShared).HasMaxLength(2000);
        builder.Property(entity => entity.InternalNotes).HasMaxLength(4000);
        builder.Property(entity => entity.RejectionReason).HasMaxLength(1000);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.EventId,
            entity.Status
        });
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.EventId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClientId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentPlan>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.PaymentPlanId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(entity => entity.RejectedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentAllocationConfiguration
    : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocations", table =>
            table.HasCheckConstraint(
                "ck_payment_allocations_amount",
                "amount > 0"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.PaymentRecordId,
            entity.PaymentInstallmentId,
            entity.ReversedAt
        });
        builder.HasOne<PaymentRecord>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.PaymentRecordId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentInstallment>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.PaymentInstallmentId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentReceiptConfiguration
    : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("payment_receipts");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.PaymentRecordId,
            entity.DocumentId
        }).IsUnique();
        builder.HasOne<PaymentRecord>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.PaymentRecordId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Modules.Documents.Domain.BasicDocument>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.OrganizationId,
                entity.DocumentId
            })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
