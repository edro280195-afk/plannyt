using System.Text.Json;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Invitations.Application;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Invitations.Security;

namespace Plannyt.Api.UnitTests.Invitations;

public sealed class InvitationDomainTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EditAfterApproval_InvalidatesApprovalAndReturnsToDraft()
    {
        var design = CreateDesign();
        var versionNumber = design.SubmitForReview(Now.AddMinutes(1));
        var version = InvitationDesignVersion.Create(
            design,
            versionNumber,
            Guid.NewGuid(),
            Now.AddMinutes(1));
        design.Approve(version.Id, Now.AddMinutes(2));

        design.UpdateDraft(
            "Nueva edición",
            design.DraftThemeJson,
            design.DraftContentJson,
            Now.AddMinutes(3));

        Assert.Equal(InvitationDesignStatus.Draft, design.Status);
        Assert.Null(design.ApprovedVersionId);
    }

    [Fact]
    public void Publish_DifferentVersionThanApproved_IsRejected()
    {
        var design = CreateDesign();
        var versionNumber = design.SubmitForReview(Now.AddMinutes(1));
        var version = InvitationDesignVersion.Create(
            design,
            versionNumber,
            Guid.NewGuid(),
            Now.AddMinutes(1));
        design.Approve(version.Id, Now.AddMinutes(2));

        Assert.Throws<DomainRuleException>(() =>
            design.Publish(Guid.NewGuid(), Now.AddMinutes(3)));
    }

    [Fact]
    public void Validator_RejectsUnknownBlockProperty()
    {
        var block = InvitationTemplateCatalog.DefaultBlocks("Evento")[0] with
        {
            Content = JsonSerializer.SerializeToElement(new
            {
                title = "Evento",
                arbitraryHtml = "<script>alert(1)</script>"
            })
        };

        var exception = Assert.Throws<RequestValidationException>(() =>
            InvitationContentValidator.Validate(
                "Diseño",
                InvitationTemplateCatalog.DefaultTheme(),
                [block]));

        Assert.Contains(exception.Errors.Keys, key => key.Contains("content"));
    }

    [Fact]
    public void Validator_RejectsJavascriptUrl()
    {
        var block = new InvitationBlockRequest(
            Guid.NewGuid(),
            InvitationBlockType.CustomButton,
            true,
            BlockVisibility.Everyone,
            null,
            0,
            JsonSerializer.SerializeToElement(new
            {
                label = "Abrir",
                url = "javascript:alert(1)"
            }),
            JsonSerializer.SerializeToElement(new
            {
                textAlign = "center"
            }));

        Assert.Throws<RequestValidationException>(() =>
            InvitationContentValidator.Validate(
                "Diseño",
                InvitationTemplateCatalog.DefaultTheme(),
                [block]));
    }

    [Fact]
    public void Validator_ReportsLowContrast()
    {
        var theme = InvitationTemplateCatalog.DefaultTheme() with
        {
            TextColor = "#FFFFFF",
            BackgroundColor = "#FFFFFF"
        };

        var result = InvitationContentValidator.Validate(
            "Diseño",
            theme,
            InvitationTemplateCatalog.DefaultBlocks("Evento"));

        Assert.Contains(result.AccessibilityWarnings, warning =>
            warning.Contains("4.5:1", StringComparison.Ordinal));
    }

    [Fact]
    public void TokenService_CreatesOpaqueTokenAndStableHash()
    {
        var service = CreateTokenService();
        var linkId = Guid.NewGuid();

        var token = service.Create(linkId);

        Assert.NotEqual(token.Value, token.Hash);
        Assert.Equal(64, token.Hash.Length);
        Assert.Equal(token.Hash, service.Hash(token.Value));
        Assert.Equal(token.Value, service.Reveal(linkId, token.DerivationKeyId));
        Assert.NotEqual(token.Value, service.Create(Guid.NewGuid()).Value);
    }

    [Fact]
    public void ReplacingLink_MakesPreviousLinkInactive()
    {
        var oldLink = GuestAccessLink.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A".PadLeft(64, 'A'),
            "default",
            Now.AddDays(7),
            Guid.NewGuid(),
            Now);
        var replacementId = Guid.NewGuid();

        oldLink.ReplaceWith(replacementId, Now.AddMinutes(1));

        Assert.Equal(GuestAccessLinkStatus.Replaced, oldLink.Status);
        Assert.Equal(replacementId, oldLink.ReplacedByLinkId);
    }

    private static GuestAccessTokenService CreateTokenService() =>
        new(Options.Create(new GuestAccessTokenOptions
        {
            ActiveKeyId = "default",
            Keys = new Dictionary<string, string>
            {
                ["default"] =
                    "unit-test-guest-link-key-with-at-least-sixty-four-characters-000000"
            }
        }));

    [Fact]
    public void Experience_SuspensionPreservesPublishedVersion()
    {
        var experience = EventGuestExperience.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Evento",
            Now);
        var designId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        experience.Publish(designId, versionId, Now.AddMinutes(1));

        experience.Suspend(Now.AddMinutes(2));
        experience.Resume(Now.AddMinutes(3));

        Assert.Equal(GuestExperienceStatus.Published, experience.Status);
        Assert.Equal(versionId, experience.ActiveVersionId);
    }

    private static InvitationDesign CreateDesign()
    {
        var theme = InvitationTemplateCatalog.DefaultTheme();
        var blocks = InvitationTemplateCatalog.DefaultBlocks("Evento");
        return InvitationDesign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Diseño",
            InvitationContentValidator.SerializeTheme(theme),
            InvitationContentValidator.SerializeBlocks(blocks),
            Guid.NewGuid(),
            Now);
    }
}
