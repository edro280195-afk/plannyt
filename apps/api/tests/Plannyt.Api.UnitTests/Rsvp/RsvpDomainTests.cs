using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.UnitTests.Rsvp;

public sealed class EventRsvpSettingsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Settings_Created_In_Draft_Status()
    {
        var settings = CreateSettings();

        Assert.Equal(RsvpSettingsStatus.Draft, settings.Status);
    }

    [Fact]
    public void Settings_Update_Draft_Valid()
    {
        var settings = CreateSettings();
        var opensAt = Now.AddDays(1);
        var closesAt = Now.AddDays(30);

        settings.UpdateDraft(
            opensAt, closesAt, "America/Mexico_City",
            true, closesAt.AddDays(-1),
            true, true,
            false, false, true, true,
            "Confirmado", "Gracias por confirmar",
            "Lamentamos tu ausencia", "El RSVP ha cerrado",
            null, "Autorizo compartir datos",
            Now.AddMinutes(1));

        Assert.Equal(opensAt, settings.OpensAt);
        Assert.Equal(closesAt, settings.ClosesAt);
        Assert.True(settings.AllowTentativeResponse);
        Assert.True(settings.AllowGroupDecline);
        Assert.Equal("Confirmado", settings.ConfirmationTitle);
    }

    [Fact]
    public void Settings_Update_Rejects_Invalid_Dates()
    {
        var settings = CreateSettings();
        var opensAt = Now.AddDays(10);
        var closesAt = Now.AddDays(5);

        Assert.Throws<DomainRuleException>(() =>
            settings.UpdateDraft(
                opensAt, closesAt, "America/Mexico_City",
                false, null,
                false, false,
                false, false, false, false,
                null, null, null, null, null, null,
                Now.AddMinutes(1)));
    }

    [Fact]
    public void Settings_Update_Rejects_ChangesCloseAt_After_ClosesAt()
    {
        var settings = CreateSettings();
        var opensAt = Now.AddDays(1);
        var closesAt = Now.AddDays(10);
        var changesCloseAt = Now.AddDays(15);

        Assert.Throws<DomainRuleException>(() =>
            settings.UpdateDraft(
                opensAt, closesAt, "America/Mexico_City",
                true, changesCloseAt,
                false, false,
                false, false, false, false,
                null, null, null, null, null, null,
                Now.AddMinutes(1)));
    }

    [Fact]
    public void Settings_Update_Rejects_When_Not_Draft_Nor_Ready()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));

        Assert.Throws<DomainRuleException>(() =>
            settings.UpdateDraft(
                null, null, "America/Mexico_City",
                false, null,
                false, false,
                false, false, false, false,
                null, null, null, null, null, null,
                Now.AddMinutes(3)));
    }

    [Fact]
    public void Settings_Mark_Ready_From_Draft()
    {
        var settings = CreateSettings();

        settings.MarkReady(Now.AddMinutes(1));

        Assert.Equal(RsvpSettingsStatus.Ready, settings.Status);
    }

    [Fact]
    public void Settings_Mark_Ready_Rejects_Non_Draft()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));

        Assert.Throws<DomainRuleException>(() =>
            settings.MarkReady(Now.AddMinutes(2)));
    }

    [Fact]
    public void Settings_Open_From_Ready()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));

        settings.Open(Now.AddMinutes(2));

        Assert.Equal(RsvpSettingsStatus.Open, settings.Status);
    }

    [Fact]
    public void Settings_Open_From_Closed()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));
        settings.Close(Now.AddMinutes(3));

        settings.Open(Now.AddMinutes(4));

        Assert.Equal(RsvpSettingsStatus.Open, settings.Status);
    }

    [Fact]
    public void Settings_Open_Rejects_From_Draft()
    {
        var settings = CreateSettings();

        Assert.Throws<DomainRuleException>(() =>
            settings.Open(Now.AddMinutes(1)));
    }

    [Fact]
    public void Settings_Close_From_Open()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));

        settings.Close(Now.AddMinutes(3));

        Assert.Equal(RsvpSettingsStatus.Closed, settings.Status);
    }

    [Fact]
    public void Settings_Close_Rejects_Non_Open()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));

        Assert.Throws<DomainRuleException>(() =>
            settings.Close(Now.AddMinutes(2)));
    }

    [Fact]
    public void Settings_Suspend_From_Open()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));

        settings.Suspend(Now.AddMinutes(3));

        Assert.Equal(RsvpSettingsStatus.Suspended, settings.Status);
    }

    [Fact]
    public void Settings_Suspend_Rejects_From_Draft()
    {
        var settings = CreateSettings();

        Assert.Throws<DomainRuleException>(() =>
            settings.Suspend(Now.AddMinutes(1)));
    }

    [Fact]
    public void Settings_Archive_From_Closed()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));
        settings.Close(Now.AddMinutes(3));

        settings.Archive(Now.AddMinutes(4));

        Assert.Equal(RsvpSettingsStatus.Archived, settings.Status);
    }

    [Fact]
    public void Settings_Archive_Rejects_Open()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));

        Assert.Throws<DomainRuleException>(() =>
            settings.Archive(Now.AddMinutes(3)));
    }

    [Fact]
    public void Settings_IsAcceptingResponses_When_Open_Within_Dates()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        var opensAt = Now.AddHours(-1);
        var closesAt = Now.AddDays(30);
        settings.UpdateDraft(
            opensAt, closesAt, "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(2));
        settings.Open(Now.AddMinutes(3));

        Assert.True(settings.IsAcceptingResponses(Now));
    }

    [Fact]
    public void Settings_IsAcceptingResponses_When_Closed()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        settings.Open(Now.AddMinutes(2));
        settings.Close(Now.AddMinutes(3));

        Assert.False(settings.IsAcceptingResponses(Now));
    }

    [Fact]
    public void Settings_IsAcceptingResponses_Before_OpensAt()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        var opensAt = Now.AddDays(5);
        var closesAt = Now.AddDays(30);
        settings.UpdateDraft(
            opensAt, closesAt, "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(2));
        settings.Open(Now.AddMinutes(3));

        Assert.False(settings.IsAcceptingResponses(Now));
    }

    [Fact]
    public void Settings_IsAcceptingResponses_After_ClosesAt()
    {
        var settings = CreateSettings();
        settings.MarkReady(Now.AddMinutes(1));
        var opensAt = Now.AddDays(-10);
        var closesAt = Now.AddHours(-1);
        settings.UpdateDraft(
            opensAt, closesAt, "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(2));
        settings.Open(Now.AddMinutes(3));

        Assert.False(settings.IsAcceptingResponses(Now));
    }

    [Fact]
    public void Settings_CanGuestModify_When_Allowed_Before_ChangesCloseAt()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            true, Now.AddDays(20),
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.True(settings.CanGuestModifyResponse(Now));
    }

    [Fact]
    public void Settings_CanGuestModify_When_Not_Allowed()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.False(settings.CanGuestModifyResponse(Now));
    }

    [Fact]
    public void Settings_CanGuestModify_When_After_ChangesCloseAt()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            true, Now.AddDays(-1),
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.False(settings.CanGuestModifyResponse(Now));
    }

    [Fact]
    public void Settings_Tentative_Enabled()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            false, null,
            true, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.True(settings.CanGuestSubmitTentative());
    }

    [Fact]
    public void Settings_Tentative_Disabled()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.False(settings.CanGuestSubmitTentative());
    }

    [Fact]
    public void Settings_Decline_Enabled()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            false, null,
            false, true,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.True(settings.CanGuestDecline());
    }

    [Fact]
    public void Settings_Decline_Disabled()
    {
        var settings = CreateSettings();
        var tomorrow = Now.AddDays(1);
        settings.UpdateDraft(
            tomorrow, Now.AddDays(30), "America/Mexico_City",
            false, null,
            false, false,
            false, false, false, false,
            null, null, null, null, null, null,
            Now.AddMinutes(1));

        Assert.False(settings.CanGuestDecline());
    }

    private static EventRsvpSettings CreateSettings() =>
        EventRsvpSettings.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "America/Mexico_City",
            Now);
}

public sealed class RsvpFormTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Form_Created_In_Draft()
    {
        var form = CreateForm();

        Assert.Equal(RsvpFormStatus.Draft, form.Status);
    }

    [Fact]
    public void Form_Submit_For_Review_From_Draft()
    {
        var form = CreateForm();

        form.SubmitForReview(Now.AddMinutes(1));

        Assert.Equal(RsvpFormStatus.InReview, form.Status);
    }

    [Fact]
    public void Form_Submit_For_Review_From_ChangesRequested()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));
        form.RequestChanges(Now.AddMinutes(2));

        form.SubmitForReview(Now.AddMinutes(3));

        Assert.Equal(RsvpFormStatus.InReview, form.Status);
    }

    [Fact]
    public void Form_Approve_From_InReview()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));

        form.Approve(Now.AddMinutes(2));

        Assert.Equal(RsvpFormStatus.Approved, form.Status);
    }

    [Fact]
    public void Form_Approve_Rejects_Non_InReview()
    {
        var form = CreateForm();

        Assert.Throws<DomainRuleException>(() =>
            form.Approve(Now.AddMinutes(1)));
    }

    [Fact]
    public void Form_Request_Changes_From_InReview()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));

        form.RequestChanges(Now.AddMinutes(2));

        Assert.Equal(RsvpFormStatus.ChangesRequested, form.Status);
    }

    [Fact]
    public void Form_Publish_From_Approved()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));
        form.Approve(Now.AddMinutes(2));
        var versionId = Guid.NewGuid();

        form.Publish(versionId, Now.AddMinutes(3));

        Assert.Equal(RsvpFormStatus.Published, form.Status);
        Assert.Equal(versionId, form.ActivePublishedVersionId);
    }

    [Fact]
    public void Form_Publish_Rejects_Non_Approved()
    {
        var form = CreateForm();

        Assert.Throws<DomainRuleException>(() =>
            form.Publish(Guid.NewGuid(), Now.AddMinutes(1)));
    }

    [Fact]
    public void Form_New_Draft_From_Published()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));
        form.Approve(Now.AddMinutes(2));
        var publishedVersionId = Guid.NewGuid();
        form.Publish(publishedVersionId, Now.AddMinutes(3));

        form.NewDraft(Now.AddMinutes(4));

        Assert.Equal(RsvpFormStatus.Draft, form.Status);
        Assert.Equal(2, form.CurrentDraftVersion);
        Assert.Equal(
            publishedVersionId,
            form.ActivePublishedVersionId);
    }

    [Fact]
    public void Form_Archive_From_Published()
    {
        var form = CreateForm();
        form.SubmitForReview(Now.AddMinutes(1));
        form.Approve(Now.AddMinutes(2));
        form.Publish(Guid.NewGuid(), Now.AddMinutes(3));

        form.Archive(Now.AddMinutes(4));

        Assert.Equal(RsvpFormStatus.Archived, form.Status);
    }

    [Fact]
    public void Form_Archive_Rejects_Draft()
    {
        var form = CreateForm();

        Assert.Throws<DomainRuleException>(() =>
            form.Archive(Now.AddMinutes(1)));
    }

    private static RsvpForm CreateForm() =>
        RsvpForm.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now);
}

public sealed class RsvpFormVersionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Version_Created_Empty_Approval()
    {
        var version = CreateVersion();

        Assert.Null(version.ApprovedBy);
        Assert.Null(version.ApprovedAt);
    }

    [Fact]
    public void Version_Approve_Sets_ApprovedBy()
    {
        var version = CreateVersion();
        var userId = Guid.NewGuid();

        version.Approve(userId, Now.AddMinutes(1));

        Assert.Equal(userId, version.ApprovedBy);
        Assert.Equal(Now.AddMinutes(1), version.ApprovedAt);
    }

    [Fact]
    public void Version_Publish_Requires_Approval()
    {
        var version = CreateVersion();

        Assert.Throws<DomainRuleException>(() =>
            version.Publish(Now.AddMinutes(1)));
    }

    [Fact]
    public void Version_Publish_After_Approve()
    {
        var version = CreateVersion();
        version.Approve(Guid.NewGuid(), Now.AddMinutes(1));

        version.Publish(Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(2), version.PublishedAt);
    }

    [Fact]
    public void Version_IsPublished_After_Publish()
    {
        var version = CreateVersion();
        version.Approve(Guid.NewGuid(), Now.AddMinutes(1));
        version.Publish(Now.AddMinutes(2));

        Assert.True(version.IsPublished);
    }

    private static RsvpFormVersion CreateVersion() =>
        RsvpFormVersion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "{}",
            "[]",
            "[]",
            "[]",
            "[]",
            Guid.NewGuid(),
            Now);
}

public sealed class RsvpSubmissionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Submission_Create_GuestPrivateLink_Rejects_Incomplete()
    {
        Assert.Throws<DomainRuleException>(() =>
            RsvpSubmission.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                RsvpSubmissionSource.GuestPrivateLink,
                RsvpOverallStatus.Incomplete,
                null,
                "Juan Pérez",
                "juan@example.com",
                null,
                "Web",
                null,
                null,
                Guid.NewGuid().ToString(),
                null,
                Now));
    }

    [Fact]
    public void Submission_Create_Manual_Incomplete_Allowed()
    {
        var submission = RsvpSubmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Incomplete,
            Guid.NewGuid(),
            "Juan Pérez",
            "juan@example.com",
            null,
            "Web",
            null,
            null,
            Guid.NewGuid().ToString(),
            null,
            Now);

        Assert.Equal(RsvpSubmissionSource.PlannerManual, submission.Source);
        Assert.Equal(RsvpOverallStatus.Incomplete, submission.OverallStatus);
    }

    [Fact]
    public void Submission_Create_Valid()
    {
        var orgId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var submission = RsvpSubmission.Create(
            orgId,
            eventId,
            groupId,
            formVersionId,
            null,
            1,
            RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Confirmed,
            userId,
            "Juan Pérez",
            "juan@example.com",
            "+528991234567",
            "Web",
            "127.0.0.1",
            "Consentimiento otorgado",
            Guid.NewGuid().ToString(),
            null,
            Now);

        Assert.Equal(orgId, submission.OrganizationId);
        Assert.Equal(eventId, submission.EventId);
        Assert.Equal(groupId, submission.InvitationGroupId);
        Assert.Equal(formVersionId, submission.RsvpFormVersionId);
        Assert.Equal(RsvpOverallStatus.Confirmed, submission.OverallStatus);
        Assert.Equal("Juan Pérez", submission.ContactNameSnapshot);
        Assert.Equal("juan@example.com", submission.ContactEmailSnapshot);
    }

    [Fact]
    public void Submission_PreviousSubmissionId_Can_Be_Set()
    {
        var previousId = Guid.NewGuid();

        var submission = RsvpSubmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            2,
            RsvpSubmissionSource.GuestPrivateLink,
            RsvpOverallStatus.Confirmed,
            null,
            "Juan Pérez",
            "juan@example.com",
            null,
            "Web",
            null,
            null,
            Guid.NewGuid().ToString(),
            previousId,
            Now);

        Assert.Equal(previousId, submission.PreviousSubmissionId);
        Assert.Equal(2, submission.RevisionNumber);
    }
}

public sealed class CurrentGuestRsvpTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CurrentGuestRsvp_Create_Valid()
    {
        var orgId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        var rsvp = CreateRsvp(orgId, eventId, groupId, guestId, submissionId);

        Assert.Equal(orgId, rsvp.OrganizationId);
        Assert.Equal(eventId, rsvp.EventId);
        Assert.Equal(groupId, rsvp.InvitationGroupId);
        Assert.Equal(guestId, rsvp.EventGuestId);
        Assert.Equal(GuestAttendanceStatus.Attending, rsvp.AttendanceStatus);
        Assert.False(rsvp.IsUnnamedCompanion);
        Assert.Equal("María García", rsvp.CurrentDisplayName);
        Assert.Equal(submissionId, rsvp.LastSubmissionId);
    }

    [Fact]
    public void CurrentGuestRsvp_Update_Status()
    {
        var rsvp = CreateRsvp(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid());

        rsvp.UpdateStatus(
            GuestAttendanceStatus.NotAttending,
            "María García López",
            Guid.NewGuid(),
            Now.AddMinutes(1));

        Assert.Equal(GuestAttendanceStatus.NotAttending, rsvp.AttendanceStatus);
        Assert.Equal("María García López", rsvp.CurrentDisplayName);
    }

    private static CurrentGuestRsvp CreateRsvp(
        Guid orgId, Guid eventId, Guid groupId,
        Guid guestId, Guid submissionId) =>
        CurrentGuestRsvp.Create(
            orgId,
            eventId,
            groupId,
            guestId,
            GuestAttendanceStatus.Attending,
            false,
            null,
            "María García",
            submissionId,
            Now);
}

public sealed class EventMenuTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Menu_Create_Valid()
    {
        var menu = CreateMenu();

        Assert.Equal("Menú Adultos", menu.Name);
        Assert.Equal(MenuCategory.AdultMeal, menu.MenuCategory);
        Assert.True(menu.IsActive);
        Assert.True(menu.SelectionRequired);
        Assert.Equal(1, menu.MinimumSelections);
        Assert.Equal(3, menu.MaximumSelections);
    }

    [Fact]
    public void Menu_Create_Rejects_Negative_Minimum()
    {
        Assert.Throws<DomainRuleException>(() =>
            EventMenu.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Menú Inválido",
                null,
                MenuCategory.AdultMeal,
                false,
                -1,
                3,
                0,
                Now));
    }

    [Fact]
    public void Menu_Create_Rejects_Minimum_Greater_Than_Maximum()
    {
        Assert.Throws<DomainRuleException>(() =>
            EventMenu.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Menú Inválido",
                null,
                MenuCategory.AdultMeal,
                false,
                5,
                3,
                0,
                Now));
    }

    [Fact]
    public void Menu_Archive_Sets_IsActive_False()
    {
        var menu = CreateMenu();

        menu.Archive(Now.AddMinutes(1));

        Assert.False(menu.IsActive);
        Assert.Equal(Now.AddMinutes(1), menu.ArchivedAt);
    }

    private static EventMenu CreateMenu() =>
        EventMenu.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Menú Adultos",
            "Platillos principales",
            MenuCategory.AdultMeal,
            true,
            1,
            3,
            0,
            Now);
}

public sealed class EventMenuOptionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MenuOption_Create_Valid()
    {
        var option = CreateOption();

        Assert.Equal("Filete de res", option.Name);
        Assert.True(option.IsActive);
        Assert.Equal(50, option.Capacity);
        Assert.Equal("gluten-free", option.DietaryTags);
    }

    [Fact]
    public void MenuOption_Create_Rejects_Negative_Capacity()
    {
        Assert.Throws<DomainRuleException>(() =>
            EventMenuOption.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Opción inválida",
                null,
                "",
                -1,
                0,
                Now));
    }

    private static EventMenuOption CreateOption() =>
        EventMenuOption.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Filete de res",
            "Acompañado de puré de papa",
            "gluten-free",
            50,
            0,
            Now);
}

public sealed class EventTransportOptionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Transport_Create_Valid()
    {
        var transport = CreateTransport();

        Assert.Equal("Autobús al evento", transport.Name);
        Assert.Equal(TransportDirection.ToCeremony, transport.Direction);
        Assert.True(transport.IsActive);
        Assert.Equal(40, transport.Capacity);
        Assert.True(transport.AllowWaitlist);
    }

    [Fact]
    public void Transport_Create_Rejects_Negative_Capacity()
    {
        Assert.Throws<DomainRuleException>(() =>
            EventTransportOption.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Transporte inválido",
                null,
                TransportDirection.ToCeremony,
                "Hotel Central",
                Now.AddDays(1),
                null,
                -5,
                false,
                0,
                Now));
    }

    private static EventTransportOption CreateTransport() =>
        EventTransportOption.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Autobús al evento",
            "Salida desde el hotel sede",
            TransportDirection.ToCeremony,
            "Hotel Central",
            Now.AddDays(1),
            null,
            40,
            true,
            0,
            Now);
}

public sealed class GuestDietaryAndAccessibilityTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Dietary_Create_Defaults_Null()
    {
        var dietary = CreateDietary();

        Assert.Null(dietary.Allergies);
        Assert.Null(dietary.DietaryRestrictions);
        Assert.Null(dietary.AccessibilityRequirements);
        Assert.Null(dietary.AdditionalNotes);
    }

    [Fact]
    public void Dietary_Update_Sets_Values()
    {
        var dietary = CreateDietary();
        var submissionId = Guid.NewGuid();

        dietary.Update(
            "Nueces, mariscos",
            "Sin gluten, vegetariana",
            "Acceso en silla de ruedas",
            "Alergia severa a nueces",
            submissionId,
            Now.AddMinutes(1));

        Assert.Equal("Nueces, mariscos", dietary.Allergies);
        Assert.Equal("Sin gluten, vegetariana", dietary.DietaryRestrictions);
        Assert.Equal("Acceso en silla de ruedas", dietary.AccessibilityRequirements);
        Assert.Equal("Alergia severa a nueces", dietary.AdditionalNotes);
        Assert.Equal(submissionId, dietary.LastSubmissionId);
    }

    [Fact]
    public void Dietary_Grant_Consent()
    {
        var dietary = CreateDietary();

        dietary.GrantConsent(Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(1), dietary.ConsentGrantedAt);
    }

    private static GuestDietaryAndAccessibility CreateDietary() =>
        GuestDietaryAndAccessibility.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now);
}

public sealed class RsvpGroupExceptionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Exception_Create_Valid()
    {
        var future = Now.AddDays(7);

        var exception = CreateException(future);

        Assert.Equal(RsvpGroupExceptionStatus.Active, exception.Status);
        Assert.Equal(future, exception.ExpiresAt);
        Assert.Equal("Grupo requiere más tiempo por viaje", exception.Reason);
    }

    [Fact]
    public void Exception_Create_Rejects_Past_Expiry()
    {
        var past = Now.AddHours(-1);

        Assert.Throws<DomainRuleException>(() =>
            CreateException(past));
    }

    [Fact]
    public void Exception_Close()
    {
        var exception = CreateException(Now.AddDays(7));

        exception.Close(Now.AddMinutes(1));

        Assert.Equal(RsvpGroupExceptionStatus.Closed, exception.Status);
        Assert.Equal(Now.AddMinutes(1), exception.ClosedAt);
    }

    [Fact]
    public void Exception_IsValid_When_Active_And_Not_Expired()
    {
        var exception = CreateException(Now.AddDays(7));

        Assert.True(exception.IsValid(Now.AddMinutes(1)));
    }

    [Fact]
    public void Exception_IsValid_When_Expired()
    {
        var exception = CreateException(Now.AddMinutes(5));

        Assert.False(exception.IsValid(Now.AddDays(1)));
    }

    [Fact]
    public void Exception_IsValid_When_Closed()
    {
        var exception = CreateException(Now.AddDays(7));
        exception.Close(Now.AddMinutes(1));

        Assert.False(exception.IsValid(Now.AddMinutes(2)));
    }

    private static RsvpGroupException CreateException(DateTimeOffset expiresAt) =>
        RsvpGroupException.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            expiresAt,
            "Grupo requiere más tiempo por viaje",
            Guid.NewGuid(),
            Now);
}

public sealed class ReminderTemplateTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Template_Create_Active()
    {
        var template = CreateTemplate();

        Assert.True(template.IsActive);
        Assert.Equal("Recordatorio 48h", template.Name);
        Assert.Equal(ReminderChannel.EmailCopy, template.Channel);
        Assert.Equal("pending_48h", template.SegmentType);
    }

    private static ReminderTemplate CreateTemplate() =>
        ReminderTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Recordatorio 48h",
            ReminderChannel.EmailCopy,
            "pending_48h",
            "Hola {{guest_name}}, recuerda confirmar tu asistencia...",
            Now);
}
