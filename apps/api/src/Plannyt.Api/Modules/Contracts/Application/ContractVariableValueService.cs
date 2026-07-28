using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Infrastructure.Persistence;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed class ContractVariableValueService(PlannytDbContext dbContext)
{
    public async Task<IReadOnlyDictionary<string, string?>> BuildAsync(
        Guid organizationId,
        Guid? eventId,
        Guid? clientId,
        Guid? proposalVersionId,
        Guid? contractId,
        string? contractNumber,
        DateTimeOffset? contractCreatedAt,
        DateTimeOffset? contractValidUntil,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == organizationId)
            .Select(entity => new
            {
                entity.Name,
                entity.CountryCode,
                entity.CurrencyCode
            })
            .SingleAsync(cancellationToken);
        values["organization.name"] = organization.Name;
        values["organization.country"] = organization.CountryCode;
        values["organization.currency"] = organization.CurrencyCode;

        if (clientId is not null)
        {
            var client = await dbContext.Clients
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == clientId)
                .Select(entity => new
                {
                    entity.DisplayName,
                    entity.PersonId
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (client is not null)
            {
                var contact = client.PersonId is not null
                    ? await dbContext.People
                        .AsNoTracking()
                        .Where(entity =>
                            entity.OrganizationId == organizationId
                            && entity.Id == client.PersonId)
                        .Select(entity => new
                        {
                            entity.DisplayName,
                            entity.ContactEmail,
                            entity.ContactPhone
                        })
                        .SingleOrDefaultAsync(cancellationToken)
                    : await dbContext.ClientContacts
                        .AsNoTracking()
                        .Where(entity =>
                            entity.OrganizationId == organizationId
                            && entity.ClientId == clientId)
                        .OrderByDescending(entity => entity.IsPrimary)
                        .Join(
                            dbContext.People.AsNoTracking(),
                            relation => new
                            {
                                relation.OrganizationId,
                                Id = relation.PersonId
                            },
                            person => new { person.OrganizationId, person.Id },
                            (_, person) => new
                            {
                                person.DisplayName,
                                person.ContactEmail,
                                person.ContactPhone
                            })
                        .FirstOrDefaultAsync(cancellationToken);
                values["client.displayName"] = client.DisplayName;
                values["client.contactName"] = contact?.DisplayName
                    ?? client.DisplayName;
                values["client.email"] = contact?.ContactEmail;
                values["client.phone"] = contact?.ContactPhone;
            }
        }

        if (eventId is not null)
        {
            var eventEntity = await dbContext.Events
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId)
                .Select(entity => new
                {
                    entity.Name,
                    entity.EventType,
                    entity.StartDateTime,
                    entity.City,
                    entity.CountryCode
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (eventEntity is not null)
            {
                values["event.name"] = eventEntity.Name;
                values["event.type"] = eventEntity.EventType;
                values["event.date"] =
                    eventEntity.StartDateTime.ToString(
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture);
                values["event.city"] = eventEntity.City;
                values["event.country"] = eventEntity.CountryCode;
            }
        }

        if (proposalVersionId is not null)
        {
            var proposal = await dbContext.ProposalVersions
                .AsNoTracking()
                .Where(version =>
                    version.OrganizationId == organizationId
                    && version.Id == proposalVersionId)
                .Join(
                    dbContext.Proposals.AsNoTracking(),
                    version => new
                    {
                        version.OrganizationId,
                        Id = version.ProposalId
                    },
                    entity => new { entity.OrganizationId, entity.Id },
                    (version, entity) => new
                    {
                        entity.ProposalNumber,
                        version.VersionNumber,
                        version.Subtotal,
                        version.DiscountTotal,
                        version.TaxTotal,
                        version.GrandTotal,
                        version.CurrencyCode
                    })
                .SingleOrDefaultAsync(cancellationToken);
            if (proposal is not null)
            {
                values["proposal.number"] = proposal.ProposalNumber;
                values["proposal.version"] =
                    proposal.VersionNumber.ToString(CultureInfo.InvariantCulture);
                values["proposal.subtotal"] = Money(proposal.Subtotal);
                values["proposal.discountTotal"] = Money(proposal.DiscountTotal);
                values["proposal.taxTotal"] = Money(proposal.TaxTotal);
                values["proposal.grandTotal"] = Money(proposal.GrandTotal);
                values["proposal.currency"] = proposal.CurrencyCode;
            }
        }

        if (contractId is not null && contractNumber is null)
        {
            var contract = await dbContext.Contracts
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == contractId)
                .Select(entity => new
                {
                    entity.ContractNumber,
                    entity.CreatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);
            contractNumber = contract?.ContractNumber;
            contractCreatedAt = contract?.CreatedAt;
        }

        values["contract.number"] = contractNumber;
        values["contract.createdAt"] = contractCreatedAt?.ToString(
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture);
        values["contract.validUntil"] = contractValidUntil?.ToString(
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture);
        return values;
    }

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.GetCultureInfo("es-MX"));
}
