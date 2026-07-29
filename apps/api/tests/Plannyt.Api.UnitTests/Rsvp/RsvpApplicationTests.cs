using System.Reflection;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Rsvp.Application;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.UnitTests.Rsvp;

public sealed class RsvpRequestFingerprintTests
{
    [Fact]
    public void Compute_Normalizes_Json_Properties_And_Guest_Order()
    {
        var firstGuestId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111");
        var secondGuestId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222");
        var first = CreateRequest(
            [
                CreateGuest(
                    secondGuestId,
                    """{"b":2,"a":1}"""),
                CreateGuest(
                    firstGuestId,
                    """{"nested":{"z":2,"a":1}}""")
            ]);
        var second = CreateRequest(
            [
                CreateGuest(
                    firstGuestId,
                    """{"nested":{"a":1,"z":2}}"""),
                CreateGuest(
                    secondGuestId,
                    """{"a":1,"b":2}""")
            ]);

        var firstFingerprint = RsvpRequestFingerprint.Compute(
            first,
            "public");
        var secondFingerprint = RsvpRequestFingerprint.Compute(
            second,
            "public");

        Assert.Equal(firstFingerprint, secondFingerprint);
        Assert.Equal(64, firstFingerprint.Length);
    }

    [Fact]
    public void Compute_Changes_When_Relevant_Content_Changes()
    {
        var original = CreateRequest(
            [CreateGuest(Guid.NewGuid(), """{"option":"A"}""")]);
        var changed = original with
        {
            ContactEmail = "otra-persona@example.invalid"
        };

        Assert.NotEqual(
            RsvpRequestFingerprint.Compute(original, "public"),
            RsvpRequestFingerprint.Compute(changed, "public"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("contains personal@example.invalid")]
    public void ValidateIdempotencyKey_Rejects_Invalid_Values(string? value)
    {
        Assert.Throws<RequestValidationException>(() =>
            RsvpRequestFingerprint.ValidateIdempotencyKey(value));
    }

    [Fact]
    public void ValidateIdempotencyKey_Returns_Client_Key()
    {
        const string key = "attempt_018fae84-f376-7e91";

        Assert.Equal(
            key,
            RsvpRequestFingerprint.ValidateIdempotencyKey(key));
    }

    private static RsvpSubmissionRequest CreateRequest(
        List<RsvpSubmissionGuestRequest> guests) =>
        new(
            3,
            RsvpOverallStatus.Confirmed,
            "  Nombre  ",
            "PERSONA@EXAMPLE.INVALID",
            "+52 899 123 4567",
            guests,
            [],
            """{"accepted":true}""");

    private static RsvpSubmissionGuestRequest CreateGuest(
        Guid eventGuestId,
        string menuJson) =>
        new(
            eventGuestId,
            "Invitado",
            "Adult",
            GuestAttendanceStatus.Attending,
            menuJson,
            "{}",
            "{}",
            "{}",
            false);
}

public sealed class RsvpAvailabilityEvaluatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Closed_Global_Window_Allows_Initial_Response_With_Active_Exception()
    {
        var settings = CreateClosedSettings(allowChanges: false);
        var exception = CreateException(Now.AddHours(1));

        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            exception,
            hasCurrentSubmission: false,
            Now);

        Assert.True(availability.CanRespond);
        Assert.True(availability.UsesGroupException);
    }

    [Fact]
    public void Closed_Global_Window_Allows_Change_When_Policy_Allows_It()
    {
        var settings = CreateClosedSettings(allowChanges: true);
        var exception = CreateException(Now.AddHours(1));

        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            exception,
            hasCurrentSubmission: true,
            Now);

        Assert.True(availability.CanRespond);
        Assert.True(availability.CanModify);
    }

    [Fact]
    public void Expired_Exception_Does_Not_Open_Response_Window()
    {
        var settings = CreateClosedSettings(allowChanges: true);
        var exception = CreateException(Now.AddMinutes(1));

        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            exception,
            hasCurrentSubmission: false,
            Now.AddMinutes(2));

        Assert.False(availability.CanRespond);
    }

    [Fact]
    public void Closed_Exception_Does_Not_Open_Response_Window()
    {
        var settings = CreateClosedSettings(allowChanges: true);
        var exception = CreateException(Now.AddHours(1));
        exception.Close(Guid.NewGuid(), Now);

        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            exception,
            hasCurrentSubmission: false,
            Now);

        Assert.False(availability.CanRespond);
    }

    [Fact]
    public void Open_Global_Window_Does_Not_Require_Exception()
    {
        var settings = CreateOpenSettings();

        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            null,
            hasCurrentSubmission: false,
            Now);

        Assert.True(availability.CanRespond);
        Assert.False(availability.UsesGroupException);
    }

    private static EventRsvpSettings CreateClosedSettings(
        bool allowChanges)
    {
        var settings = CreateSettings(allowChanges);
        settings.MarkReady(Now.AddMinutes(-4));
        settings.Open(Now.AddMinutes(-3));
        settings.Close(Now.AddMinutes(-2));
        return settings;
    }

    private static EventRsvpSettings CreateOpenSettings()
    {
        var settings = CreateSettings(allowChanges: false);
        settings.MarkReady(Now.AddMinutes(-2));
        settings.Open(Now.AddMinutes(-1));
        return settings;
    }

    private static EventRsvpSettings CreateSettings(bool allowChanges)
    {
        var settings = EventRsvpSettings.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "America/Matamoros",
            Now.AddMinutes(-10));
        settings.UpdateDraft(
            Now.AddDays(-1),
            Now.AddDays(1),
            "America/Matamoros",
            allowChanges,
            Now.AddHours(2),
            false,
            true,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            Now.AddMinutes(-5));
        return settings;
    }

    private static RsvpGroupException CreateException(
        DateTimeOffset expiresAt) =>
        RsvpGroupException.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            expiresAt,
            "Atención autorizada",
            Guid.NewGuid(),
            Now.AddMinutes(-1));
}

public sealed class RsvpSubmissionConcurrencyPolicyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Same_Fingerprint_Returns_Existing_Submission()
    {
        var submission = CreateSubmission(
            1,
            null,
            RsvpSubmissionSource.GuestPrivateLink,
            new string('A', 64));

        var resolved =
            RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                submission,
                submission.RequestFingerprint);

        Assert.Equal(submission.Id, resolved);
    }

    [Fact]
    public void Different_Fingerprint_Is_Idempotency_Conflict()
    {
        var submission = CreateSubmission(
            1,
            null,
            RsvpSubmissionSource.GuestPrivateLink,
            new string('A', 64));

        Assert.Throws<IdempotencyConflictException>(() =>
            RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                submission,
                new string('B', 64)));
    }

    [Fact]
    public void Next_Revision_Is_Incremental_And_Chained()
    {
        var previous = CreateSubmission(
            4,
            Guid.NewGuid(),
            RsvpSubmissionSource.PlannerManual,
            new string('A', 64));

        var reservation =
            RsvpSubmissionConcurrencyPolicy.ReserveNextRevision(previous);

        Assert.Equal(5, reservation.RevisionNumber);
        Assert.Equal(previous.Id, reservation.PreviousSubmissionId);
    }

    [Fact]
    public void Stale_Expected_Revision_Is_Conflict()
    {
        var current = CreateSubmission(
            2,
            Guid.NewGuid(),
            RsvpSubmissionSource.PlannerManual,
            new string('A', 64));

        var conflict = Assert.Throws<RsvpRevisionConflictException>(() =>
            RsvpSubmissionConcurrencyPolicy.ValidateExpectedRevision(
                1,
                current));

        Assert.Equal(1, conflict.ExpectedRevision);
        Assert.Equal(2, conflict.CurrentRevision);
    }

    [Fact]
    public void SupportCorrection_Preserves_Source_And_Previous_Submission()
    {
        var previous = CreateSubmission(
            1,
            null,
            RsvpSubmissionSource.PlannerManual,
            new string('A', 64));
        var reservation =
            RsvpSubmissionConcurrencyPolicy.ReserveNextRevision(previous);
        var correction = CreateSubmission(
            reservation.RevisionNumber,
            reservation.PreviousSubmissionId,
            RsvpSubmissionSource.SupportCorrection,
            new string('B', 64));

        Assert.Equal(
            RsvpSubmissionSource.SupportCorrection,
            correction.Source);
        Assert.Equal(previous.Id, correction.PreviousSubmissionId);
        Assert.Equal(2, correction.RevisionNumber);
    }

    private static RsvpSubmission CreateSubmission(
        int revision,
        Guid? previousSubmissionId,
        RsvpSubmissionSource source,
        string fingerprint) =>
        RsvpSubmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            revision,
            source,
            RsvpOverallStatus.Confirmed,
            Guid.NewGuid(),
            "Contacto",
            null,
            null,
            null,
            null,
            null,
            $"attempt-unit-{Guid.NewGuid():N}",
            previousSubmissionId,
            Now,
            fingerprint);
}

public sealed class RsvpTransportAllocationPolicyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null, 500)]
    [InlineData(2, 1)]
    public void Available_Or_Unlimited_Capacity_Confirms(
        int? capacity,
        int confirmedCount)
    {
        var status = RsvpTransportAllocationPolicy.DetermineStatus(
            "Transporte",
            capacity,
            confirmedCount,
            allowWaitlist: false);

        Assert.Equal(TransportSelectionStatus.Confirmed, status);
    }

    [Fact]
    public void Full_Capacity_Uses_Waitlist_When_Enabled()
    {
        var status = RsvpTransportAllocationPolicy.DetermineStatus(
            "Transporte",
            1,
            1,
            allowWaitlist: true);

        Assert.Equal(TransportSelectionStatus.Waitlisted, status);
    }

    [Fact]
    public void Full_Capacity_Rejects_When_Waitlist_Is_Disabled()
    {
        Assert.Throws<ConflictException>(() =>
            RsvpTransportAllocationPolicy.DetermineStatus(
                "Transporte",
                1,
                1,
                allowWaitlist: false));
        Assert.False(
            RsvpTransportAllocationPolicy.CanAllocate(
                1,
                1,
                allowWaitlist: false));
    }

    [Fact]
    public void Promotion_Selects_Lowest_Waitlist_Sequence()
    {
        var organizationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var later = GuestTransportSelection.Create(
            organizationId,
            eventId,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            optionId,
            TransportSelectionStatus.Waitlisted,
            submissionId,
            2,
            Now);
        var first = GuestTransportSelection.Create(
            organizationId,
            eventId,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            optionId,
            TransportSelectionStatus.Waitlisted,
            submissionId,
            1,
            Now.AddMinutes(1));

        var selected =
            RsvpTransportAllocationPolicy.SelectNextWaitlisted(
                [later, first]);

        Assert.Same(first, selected);
    }
}

public sealed class AuditActionCatalogTests
{
    [Fact]
    public void Corrected_Modules_Do_Not_Disperse_Canonical_Action_Strings()
    {
        var repositoryRoot = FindRepositoryRoot();
        var catalogPath = Path.Combine(
            repositoryRoot,
            "apps",
            "api",
            "src",
            "Plannyt.Api",
            "Modules",
            "Audit",
            "Domain",
            "AuditActions.cs");
        var files = Directory
            .EnumerateFiles(
                Path.Combine(
                    repositoryRoot,
                    "apps",
                    "api",
                    "src",
                    "Plannyt.Api",
                    "Modules",
                    "Rsvp"),
                "*.cs",
                SearchOption.AllDirectories)
            .Concat(
                [
                    Path.Combine(
                        repositoryRoot,
                        "apps",
                        "api",
                        "src",
                        "Plannyt.Api",
                        "Modules",
                        "Invitations",
                        "Application",
                        "GuestLinkService.cs"),
                    Path.Combine(
                        repositoryRoot,
                        "apps",
                        "api",
                        "src",
                        "Plannyt.Api",
                        "Modules",
                        "Invitations",
                        "Application",
                        "PortalGuestCollaborationService.cs")
                ])
            .Where(path =>
                !string.Equals(
                    path,
                    catalogPath,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        var actionValues = typeof(AuditActions)
            .GetFields(
                BindingFlags.Public
                | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AuditAction))
            .Select(field => ((AuditAction)field.GetValue(null)!).Value)
            .ToList();

        var violations = files
            .SelectMany(path =>
                actionValues
                    .Where(action =>
                        File.ReadAllText(path).Contains(
                            $"\"{action}\"",
                            StringComparison.Ordinal))
                    .Select(action =>
                        $"{Path.GetRelativePath(repositoryRoot, path)}: {action}"))
            .ToList();

        Assert.Empty(violations);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !Directory.Exists(
                   Path.Combine(directory.FullName, "apps")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException(
                   "No se encontró la raíz del repositorio.");
    }
}

public sealed class RsvpSensitivePermissionTests
{
    [Fact]
    public void Planner_Does_Not_Receive_Sensitive_Permissions_By_Default()
    {
        var permissions = RolePermissionCatalog.GetFor(
            OrganizationRole.Planner);

        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataView,
            permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataManage,
            permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataExport,
            permissions);
    }

    [Theory]
    [InlineData(OrganizationRole.Owner)]
    [InlineData(OrganizationRole.OrganizationAdmin)]
    public void Privileged_Roles_Receive_Sensitive_Permissions(
        OrganizationRole role)
    {
        var permissions = RolePermissionCatalog.GetFor(role);

        Assert.Contains(
            Permissions.GuestSensitiveDataView,
            permissions);
        Assert.Contains(
            Permissions.GuestSensitiveDataManage,
            permissions);
        Assert.Contains(
            Permissions.GuestSensitiveDataExport,
            permissions);
    }

    [Theory]
    [InlineData(EventAccessRole.ClientPrimary)]
    [InlineData(EventAccessRole.ClientCollaborator)]
    public void Portal_Roles_Can_Capture_But_Do_Not_Receive_Sensitive_Permissions(
        EventAccessRole role)
    {
        var permissions = RolePermissionCatalog.GetFor(role);

        Assert.Contains(
            Permissions.RsvpResponsesCreateManual,
            permissions);
        Assert.Contains(
            Permissions.RsvpResponsesCorrect,
            permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataView,
            permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataManage,
            permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataExport,
            permissions);
    }
}
