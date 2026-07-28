using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Modules.Invitations.Application;

namespace Plannyt.Api.Infrastructure.Persistence;

public static class InvitationTemplateInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var templates = InvitationTemplateCatalog.CreateDefaults();
        var ids = templates.Select(template => template.Id).ToList();
        var existing = await dbContext.InvitationTemplates
            .Where(template => ids.Contains(template.Id))
            .Select(template => template.Id)
            .ToListAsync();
        dbContext.InvitationTemplates.AddRange(
            templates.Where(template => !existing.Contains(template.Id)));
        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
