using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Payments.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Infrastructure.Persistence;

public sealed class PlannytDbContext(DbContextOptions<PlannytDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<OrganizationMembership> OrganizationMemberships =>
        Set<OrganizationMembership>();

    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();

    public DbSet<Prospect> Prospects => Set<Prospect>();

    public DbSet<ProspectStatusHistory> ProspectStatusHistory =>
        Set<ProspectStatusHistory>();

    public DbSet<ProspectActivity> ProspectActivities => Set<ProspectActivity>();

    public DbSet<ServiceCatalogItem> ServiceCatalogItems =>
        Set<ServiceCatalogItem>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PackageItem> PackageItems => Set<PackageItem>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<Proposal> Proposals => Set<Proposal>();

    public DbSet<ProposalDraftLine> ProposalDraftLines => Set<ProposalDraftLine>();

    public DbSet<ProposalVersion> ProposalVersions => Set<ProposalVersion>();

    public DbSet<ProposalLine> ProposalLines => Set<ProposalLine>();

    public DbSet<ProposalComment> ProposalComments => Set<ProposalComment>();

    public DbSet<ProposalShareLink> ProposalShareLinks => Set<ProposalShareLink>();

    public DbSet<ContractTemplate> ContractTemplates => Set<ContractTemplate>();

    public DbSet<OrganizationContractingPolicy> OrganizationContractingPolicies =>
        Set<OrganizationContractingPolicy>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ContractingRequirementSnapshot> ContractingRequirementSnapshots =>
        Set<ContractingRequirementSnapshot>();

    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    public DbSet<ContractParty> ContractParties => Set<ContractParty>();

    public DbSet<ContractSigner> ContractSigners => Set<ContractSigner>();

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();

    public DbSet<SignatureEvidence> SignatureEvidence => Set<SignatureEvidence>();

    public DbSet<ContractFinalDocument> ContractFinalDocuments =>
        Set<ContractFinalDocument>();

    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();

    public DbSet<PaymentInstallment> PaymentInstallments =>
        Set<PaymentInstallment>();

    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventStatusHistory> EventStatusHistory => Set<EventStatusHistory>();

    public DbSet<EventClient> EventClients => Set<EventClient>();

    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    public DbSet<EventAccess> EventAccesses => Set<EventAccess>();

    public DbSet<AccessInvitation> AccessInvitations => Set<AccessInvitation>();

    public DbSet<BasicDocument> BasicDocuments => Set<BasicDocument>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannytDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ValidateImmutableEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ValidateImmutableEntries()
    {
        if (ChangeTracker.Entries<SignatureEvidence>().Any(
                entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "La evidencia de firma es inmutable.");
        }

        foreach (var entry in ChangeTracker.Entries<ContractVersion>()
                     .Where(entry =>
                         entry.State is EntityState.Modified or EntityState.Deleted))
        {
            var publishedAt = entry.OriginalValues
                .GetValue<DateTimeOffset?>(nameof(ContractVersion.PublishedAt));
            var isOnlySuperseding = entry.State == EntityState.Modified
                && entry.Properties
                    .Where(property => property.IsModified)
                    .All(property =>
                        property.Metadata.Name
                            == nameof(ContractVersion.SupersededAt));
            if (publishedAt is not null && !isOnlySuperseding)
            {
                throw new InvalidOperationException(
                    "Una versión de contrato publicada es inmutable.");
            }
        }
    }
}
