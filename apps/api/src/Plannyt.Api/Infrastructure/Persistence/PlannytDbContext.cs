using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
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
}
