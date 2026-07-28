using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Infrastructure.Persistence;

namespace Plannyt.Api.Modules.Identity.Application;

public sealed partial class OrganizationSlugGenerator(PlannytDbContext dbContext)
{
    public async Task<string> GenerateAsync(
        string organizationName,
        CancellationToken cancellationToken)
    {
        var baseSlug = BuildBaseSlug(organizationName);
        var candidate = baseSlug;

        while (await dbContext.Organizations.AnyAsync(
                   entity => entity.Slug == candidate,
                   cancellationToken))
        {
            candidate = $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(
                baseSlug.Length + 9,
                100)];
        }

        return candidate;
    }

    private static string BuildBaseSlug(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized
            .Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            .ToArray());
        var slug = InvalidSlugCharacters()
            .Replace(withoutMarks.ToLowerInvariant(), "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "organizacion";
        }

        return slug[..Math.Min(slug.Length, 90)];
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidSlugCharacters();
}
