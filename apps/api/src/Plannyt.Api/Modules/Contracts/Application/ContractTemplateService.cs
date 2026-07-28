using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Contracts.Rendering;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed class ContractTemplateService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    ContractTemplateRenderer renderer,
    ContractVariableValueService variableValueService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ContractTemplateResponse>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractTemplatesView,
            null,
            cancellationToken);
        return await dbContext.ContractTemplates
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ArchivedAt == null)
            .OrderByDescending(entity => entity.IsDefault)
            .ThenBy(entity => entity.Name)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContractTemplateResponse> CreateAsync(
        Guid organizationId,
        UpsertContractTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractTemplatesManage,
            null,
            cancellationToken);
        Validate(request);
        var now = timeProvider.GetUtcNow();
        if (request.IsDefault)
        {
            await ClearDefaultAsync(organizationId, now, cancellationToken);
        }

        var template = ContractTemplate.Create(
            organizationId,
            request.Name.Trim(),
            Normalize(request.Description),
            renderer.Sanitize(request.Content.Trim()),
            request.IsDefault,
            access.UserAccountId,
            now);
        dbContext.ContractTemplates.Add(template);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "contract_template.created",
            nameof(ContractTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(template);
    }

    public async Task<ContractTemplateResponse> UpdateAsync(
        Guid organizationId,
        Guid templateId,
        UpsertContractTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractTemplatesManage,
            null,
            cancellationToken);
        Validate(request);
        var template = await FindAsync(
            organizationId,
            templateId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (request.IsDefault)
        {
            await ClearDefaultAsync(
                organizationId,
                now,
                cancellationToken,
                templateId);
        }

        template.Update(
            request.Name.Trim(),
            Normalize(request.Description),
            renderer.Sanitize(request.Content.Trim()),
            request.IsDefault,
            request.IsActive,
            now);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "contract_template.updated",
            nameof(ContractTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(template);
    }

    public async Task ArchiveAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractTemplatesManage,
            null,
            cancellationToken);
        var template = await FindAsync(
            organizationId,
            templateId,
            cancellationToken);
        template.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "contract_template.archived",
            nameof(ContractTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContractTemplatePreviewResponse> PreviewAsync(
        Guid organizationId,
        PreviewContractTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractTemplatesView,
            request.EventId,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw Validation("content", "El contenido es obligatorio.");
        }

        var values = await variableValueService.BuildAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            request.ProposalVersionId,
            request.ContractId,
            null,
            null,
            request.ValidUntil,
            cancellationToken);
        var result = renderer.Render(request.Content, values);
        return new ContractTemplatePreviewResponse(
            result.RenderedContent,
            result.UnknownVariables,
            result.MissingVariables,
            result.CanPublish);
    }

    private async Task ClearDefaultAsync(
        Guid organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        Guid? exceptId = null)
    {
        var current = await dbContext.ContractTemplates
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.IsDefault
                && entity.Id != exceptId)
            .ToListAsync(cancellationToken);
        foreach (var template in current)
        {
            template.Update(
                template.Name,
                template.Description,
                template.Content,
                false,
                template.IsActive,
                now);
        }
    }

    private async Task<ContractTemplate> FindAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken) =>
        await dbContext.ContractTemplates.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == templateId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró la plantilla.");

    private static void Validate(UpsertContractTemplateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 200)
        {
            errors["name"] = ["El nombre es obligatorio y admite 200 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors["content"] = ["El contenido es obligatorio."];
        }

        if (request.Description?.Trim().Length > 2000)
        {
            errors["description"] = ["La descripción admite 2,000 caracteres."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static ContractTemplateResponse ToResponse(
        ContractTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.Description,
            template.Content,
            template.ContentFormat,
            template.IsDefault,
            template.IsActive,
            template.CreatedAt,
            template.UpdatedAt,
            template.ArchivedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
