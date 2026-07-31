using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.IntegrationTests.Contracting;

[Collection(ApiCollection.Name)]
public sealed class ContractingFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task AcceptedProposal_ToConfirmedEvent_CompletesContractingFlow()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "contracting-flow");
        var context = await SeedAcceptedProposalAsync(session);

        var contract = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/from-proposal",
            new
            {
                proposalId = context.ProposalId,
                name = "Contrato de coordinación integral",
                templateId = (Guid?)null,
                content = (string?)null,
                consentText =
                    "Declaro que revisé el documento y acepto utilizar medios electrónicos.",
                validUntil = DateTimeOffset.UtcNow.AddDays(7)
            });
        var contractId = contract.GetProperty("id").GetGuid();
        var parties = contract.GetProperty("parties").EnumerateArray().ToList();
        var clientPartyId = parties.Single(item =>
            item.GetProperty("partyType").GetString() == "Client")
            .GetProperty("id")
            .GetGuid();
        var plannerPartyId = parties.Single(item =>
            item.GetProperty("partyType").GetString() == "PlannerOrganization")
            .GetProperty("id")
            .GetGuid();

        var published = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/publish",
            new { });
        Assert.Equal(64, published.GetProperty("documentSha256").GetString()?.Length);

        var clientSigner = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers",
            new
            {
                contractPartyId = clientPartyId,
                personId = context.ClientPersonId,
                userAccountId = (Guid?)null,
                name = "Ana López",
                email = "ana@example.invalid",
                signerRole = "Cliente contratante",
                signingOrder = 1,
                isRequired = true
            });
        var plannerSigner = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers",
            new
            {
                contractPartyId = plannerPartyId,
                personId = (Guid?)null,
                userAccountId = session.UserAccountId,
                name = "Mariana Torres",
                email = session.Email,
                signerRole = "Representante de la organización",
                signingOrder = 2,
                isRequired = true
            });
        var clientSignerId = clientSigner.GetProperty("id").GetGuid();
        var plannerSignerId = plannerSigner.GetProperty("id").GetGuid();

        var link = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers/{clientSignerId}/requests",
            new { expiresAt = (DateTimeOffset?)null });
        var url = link.GetProperty("signingUrl").GetString()
            ?? throw new InvalidOperationException("No se generó el enlace.");
        var token = url[(url.LastIndexOf('/') + 1)..];

        using (var publicContract = await factory.CreateClient().GetAsync(
            $"/api/public/signatures/{token}"))
        {
            publicContract.EnsureSuccessStatusCode();
            var payload = await publicContract.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                published.GetProperty("documentSha256").GetString(),
                payload.GetProperty("documentSha256").GetString());
            Assert.False(payload.TryGetProperty("ipAddress", out _));
        }

        using (var publicSignature = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/signatures/{token}/sign",
            new
            {
                signingMethod = "Typed",
                declaredSignerName = "Ana LÃ³pez",
                acceptElectronicMeans = false,
                confirmDisplayedVersion = true,
                signatureDataUrl = (string?)null
            }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, publicSignature.StatusCode);
        }

        using (var publicSignature = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/signatures/{token}/sign",
            new
            {
                signingMethod = "Typed",
                declaredSignerName = "Ana López",
                acceptElectronicMeans = true,
                confirmDisplayedVersion = true,
                signatureDataUrl = (string?)null
            }))
        {
            publicSignature.EnsureSuccessStatusCode();
        }

        var completed = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers/{plannerSignerId}/sign",
            new
            {
                signingMethod = "AuthenticatedConfirmation",
                declaredSignerName = "Mariana Torres",
                acceptElectronicMeans = true,
                confirmDisplayedVersion = true,
                signatureDataUrl = (string?)null
            });
        Assert.Equal("Completed", completed.GetProperty("status").GetString());
        using (var editCompleted = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/draft")
        {
            Content = JsonContent.Create(new
            {
                name = "Cambio silencioso",
                templateId = (Guid?)null,
                content = "<p>Contenido alterado</p>",
                consentText = "Consentimiento alterado",
                validUntil = DateTimeOffset.UtcNow.AddDays(8)
            })
        })
        {
            editCompleted.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var response = await factory.CreateClient().SendAsync(
                editCompleted);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using (var finalPdfRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/final",
            session.AccessToken))
        using (var finalPdf = await factory.CreateClient().SendAsync(
            finalPdfRequest))
        {
            finalPdf.EnsureSuccessStatusCode();
            Assert.Equal(
                "application/pdf",
                finalPdf.Content.Headers.ContentType?.MediaType);
            Assert.True((await finalPdf.Content.ReadAsByteArrayAsync()).Length > 0);
        }

        var evidence = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/evidence");
        Assert.Equal(2, evidence.GetArrayLength());
        Assert.All(
            evidence.EnumerateArray(),
            item => Assert.Equal(
                published.GetProperty("documentSha256").GetString(),
                item.GetProperty("documentSha256").GetString()));

        using (var reusedToken = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/signatures/{token}/sign",
            new
            {
                signingMethod = "Typed",
                declaredSignerName = "Ana López",
                acceptElectronicMeans = true,
                confirmDisplayedVersion = true,
                signatureDataUrl = (string?)null
            }))
        {
            Assert.Equal(HttpStatusCode.Gone, reusedToken.StatusCode);
        }

        var plan = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/payment-plans",
            new
            {
                eventId = context.EventId,
                clientId = context.ClientId,
                contractId,
                proposalVersionId = context.ProposalVersionId,
                currencyCode = "MXN",
                totalAmount = 10000m,
                installments = new[]
                {
                    new
                    {
                        sequenceNumber = 1,
                        description = "Anticipo",
                        dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                        amount = 2000m,
                        installmentType = "Deposit"
                    },
                    new
                    {
                        sequenceNumber = 2,
                        description = "Pago final",
                        dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(5)),
                        amount = 8000m,
                        installmentType = "FinalPayment"
                    }
                }
            });
        var planId = plan.GetProperty("id").GetGuid();
        var depositId = plan.GetProperty("installments")[0]
            .GetProperty("id")
            .GetGuid();
        _ = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/payment-plans/{planId}/activate",
            new { });

        var payment = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/payments",
            new
            {
                eventId = context.EventId,
                clientId = context.ClientId,
                paymentPlanId = planId,
                paymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                amount = 2000m,
                currencyCode = "MXN",
                method = "BankTransfer",
                reference = "SPEI-123",
                notesShared = "Anticipo recibido",
                internalNotes = "Verificado en cuenta bancaria"
            });
        var paymentId = payment.GetProperty("id").GetGuid();
        _ = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/payments/{paymentId}/approve",
            new { });
        using (var overAllocation = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/payments/{paymentId}/allocations")
        {
            Content = JsonContent.Create(new[]
            {
                new
                {
                    paymentInstallmentId = depositId,
                    amount = 2000.01m
                }
            })
        })
        {
            overAllocation.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var response = await factory.CreateClient().SendAsync(
                overAllocation);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        _ = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/payments/{paymentId}/allocations",
            new[]
            {
                new
                {
                    paymentInstallmentId = depositId,
                    amount = 2000m
                }
            });

        var readiness = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/events/{context.EventId}/contracting-readiness");
        Assert.True(readiness.GetProperty("proposalAccepted").GetBoolean());
        Assert.True(readiness.GetProperty("contractCompleted").GetBoolean());
        Assert.True(readiness.GetProperty("depositSatisfied").GetBoolean());
        Assert.True(readiness.GetProperty("readyForConfirmation").GetBoolean());

        var confirmed = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/events/{context.EventId}/confirm",
            new { });
        Assert.Equal("Confirmed", confirmed.GetProperty("eventStatus").GetString());
        var confirmedAgain = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/events/{context.EventId}/confirm",
            new { });
        Assert.Equal(
            "Confirmed",
            confirmedAgain.GetProperty("eventStatus").GetString());
    }

    [Fact]
    public async Task ContractingReadiness_WithoutContract_DescribesRequirementAsPendingNotDone()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "contracting-readiness-wording");
        var context = await SeedAcceptedProposalAsync(session);

        var readiness = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/events/{context.EventId}/contracting-readiness");

        var missing = readiness.GetProperty("missingRequirements")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToList();
        Assert.False(readiness.GetProperty("readyForConfirmation").GetBoolean());
        Assert.Contains("Contrato por completar", missing);
        Assert.DoesNotContain("Contrato completado", missing);
        Assert.DoesNotContain("Anticipo cubierto", missing);

        using var confirmAttempt = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{context.EventId}/confirm")
        {
            Content = JsonContent.Create(new { })
        };
        confirmAttempt.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(confirmAttempt);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var detail = problem.GetProperty("detail").GetString();
        Assert.Contains("Contrato por completar", detail);
        Assert.DoesNotContain("Contrato completado", detail);
    }

    [Fact]
    public async Task RevokeRequest_Decline_AndCancel_UpdateContractAndSignerStateCorrectly()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "contracting-cancel-revoke");
        var context = await SeedAcceptedProposalAsync(session);

        var contract = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/from-proposal",
            new
            {
                proposalId = context.ProposalId,
                name = "Contrato de coordinación integral",
                templateId = (Guid?)null,
                content = (string?)null,
                consentText =
                    "Declaro que revisé el documento y acepto utilizar medios electrónicos.",
                validUntil = DateTimeOffset.UtcNow.AddDays(7)
            });
        var contractId = contract.GetProperty("id").GetGuid();
        var parties = contract.GetProperty("parties").EnumerateArray().ToList();
        var clientPartyId = parties.Single(item =>
            item.GetProperty("partyType").GetString() == "Client")
            .GetProperty("id")
            .GetGuid();
        var plannerPartyId = parties.Single(item =>
            item.GetProperty("partyType").GetString() == "PlannerOrganization")
            .GetProperty("id")
            .GetGuid();

        _ = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/publish",
            new { });

        var clientSigner = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers",
            new
            {
                contractPartyId = clientPartyId,
                personId = context.ClientPersonId,
                userAccountId = (Guid?)null,
                name = "Ana López",
                email = "ana@example.invalid",
                signerRole = "Cliente contratante",
                signingOrder = 1,
                isRequired = true
            });
        var clientSignerId = clientSigner.GetProperty("id").GetGuid();
        var plannerSigner = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers",
            new
            {
                contractPartyId = plannerPartyId,
                personId = (Guid?)null,
                userAccountId = session.UserAccountId,
                name = "Mariana Torres",
                email = session.Email,
                signerRole = "Representante de la organización",
                signingOrder = 2,
                isRequired = true
            });
        var plannerSignerId = plannerSigner.GetProperty("id").GetGuid();

        var beforeAnyRequest = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}");
        Assert.All(
            beforeAnyRequest.GetProperty("signers").EnumerateArray(),
            signer => Assert.Equal(
                JsonValueKind.Null,
                signer.GetProperty("activeSignatureRequestId").ValueKind));

        var firstLink = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers/{clientSignerId}/requests",
            new { expiresAt = (DateTimeOffset?)null });
        var firstRequestId = firstLink.GetProperty("id").GetGuid();
        var firstUrl = firstLink.GetProperty("signingUrl").GetString()
            ?? throw new InvalidOperationException("No se generó el enlace.");
        var firstToken = firstUrl[(firstUrl.LastIndexOf('/') + 1)..];

        var afterFirstRequest = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}");
        var clientSignerAfterRequest = afterFirstRequest.GetProperty("signers")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == clientSignerId);
        Assert.Equal(
            firstRequestId,
            clientSignerAfterRequest.GetProperty("activeSignatureRequestId").GetGuid());

        using (var revoke = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/requests/{firstRequestId}",
            session.AccessToken))
        using (var revokeResponse = await factory.CreateClient().SendAsync(revoke))
        {
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        }

        var afterRevoke = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}");
        var clientSignerAfterRevoke = afterRevoke.GetProperty("signers")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == clientSignerId);
        Assert.Equal(
            JsonValueKind.Null,
            clientSignerAfterRevoke.GetProperty("activeSignatureRequestId").ValueKind);

        using (var revokedTokenResponse = await factory.CreateClient().GetAsync(
            $"/api/public/signatures/{firstToken}"))
        {
            Assert.Equal(HttpStatusCode.Gone, revokedTokenResponse.StatusCode);
        }

        var secondLink = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers/{clientSignerId}/requests",
            new { expiresAt = (DateTimeOffset?)null });
        var secondUrl = secondLink.GetProperty("signingUrl").GetString()
            ?? throw new InvalidOperationException("No se generó el enlace.");
        var secondToken = secondUrl[(secondUrl.LastIndexOf('/') + 1)..];

        using (var decline = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/signatures/{secondToken}/decline",
            new { reason = "No estoy de acuerdo con el clausulado de pago." }))
        {
            Assert.Equal(HttpStatusCode.NoContent, decline.StatusCode);
        }

        var afterDecline = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}");
        Assert.Equal("Declined", afterDecline.GetProperty("status").GetString());

        using (var blockedSignAttempt = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/signers/{plannerSignerId}/sign")
        {
            Content = JsonContent.Create(new
            {
                signingMethod = "AuthenticatedConfirmation",
                declaredSignerName = "Mariana Torres",
                acceptElectronicMeans = true,
                confirmDisplayedVersion = true,
                signatureDataUrl = (string?)null
            })
        })
        {
            blockedSignAttempt.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var blockedSignResponse = await factory.CreateClient().SendAsync(
                blockedSignAttempt);
            Assert.Equal(HttpStatusCode.BadRequest, blockedSignResponse.StatusCode);
        }

        const string cancellationReason = "El cliente rechazó el contrato.";
        using (var cancel = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}/cancel")
        {
            Content = JsonContent.Create(new { reason = cancellationReason })
        })
        {
            cancel.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var cancelResponse = await factory.CreateClient().SendAsync(cancel);
            Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);
        }

        var afterCancel = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contracts/{contractId}");
        Assert.Equal("Cancelled", afterCancel.GetProperty("status").GetString());
        Assert.Equal(
            cancellationReason,
            afterCancel.GetProperty("cancellationReason").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterCancel.GetProperty("cancelledAt").ValueKind);
    }

    [Fact]
    public async Task ArchiveContractTemplate_RemovesItFromTheActiveLibrary()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "contract-template-archive");

        var template = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contract-templates",
            new
            {
                name = "Plantilla temporal",
                description = "Desechable",
                content = "<p>{{organization.name}}</p>",
                isDefault = false,
                isActive = true
            });
        var templateId = template.GetProperty("id").GetGuid();

        var beforeArchive = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contract-templates");
        Assert.Contains(
            beforeArchive.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == templateId);

        using (var archiveRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{session.OrganizationId}/contract-templates/{templateId}",
            session.AccessToken))
        using (var archiveResponse = await factory.CreateClient().SendAsync(archiveRequest))
        {
            Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
        }

        var afterArchive = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/contract-templates");
        Assert.DoesNotContain(
            afterArchive.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == templateId);
    }

    [Fact]
    public async Task PublicSignature_WithInvalidToken_ReturnsNotFound()
    {
        using var response = await factory.CreateClient().GetAsync(
            "/api/public/signatures/not-a-valid-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<SeededContext> SeedAcceptedProposalAsync(
        TestSession session)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var now = DateTimeOffset.UtcNow;
        var clientPerson = Person.Create(
            session.OrganizationId,
            null,
            "Ana",
            "López",
            "Ana López",
            "ana@example.invalid",
            "+528991234567",
            "es",
            "America/Matamoros",
            now);
        var client = Client.CreatePerson(
            session.OrganizationId,
            clientPerson.Id,
            clientPerson.DisplayName,
            "Integración",
            now);
        var eventEntity = Event.Create(
            session.OrganizationId,
            "Boda Ana y Carlos",
            "Wedding",
            now.AddMonths(6),
            now.AddMonths(6).AddHours(8),
            "America/Matamoros",
            "Matamoros",
            "MX",
            "Celebración familiar",
            120,
            session.UserAccountId,
            now);
        var eventClient = EventClient.Create(
            session.OrganizationId,
            eventEntity.Id,
            client.Id,
            EventClientRelationshipType.ContractingClient,
            true,
            true,
            now);
        var proposal = Proposal.Create(
            session.OrganizationId,
            null,
            client.Id,
            eventEntity.Id,
            $"P-{Guid.NewGuid():N}",
            "MXN",
            now.AddDays(30),
            "Propuesta integral",
            "Condiciones comerciales",
            null,
            DiscountType.None,
            0m,
            null,
            session.UserAccountId,
            now);
        dbContext.AddRange(
            clientPerson,
            client,
            eventEntity,
            eventClient,
            proposal);
        await dbContext.SaveChangesAsync();

        var version = ProposalVersion.Create(
            session.OrganizationId,
            proposal.Id,
            1,
            8620.69m,
            0m,
            1379.31m,
            10000m,
            "MXN",
            now.AddDays(30),
            "Propuesta integral",
            "Condiciones comerciales",
            DiscountType.None,
            0m,
            0m,
            null,
            null,
            0m,
            session.UserAccountId,
            now);
        dbContext.ProposalVersions.Add(version);
        proposal.RecordPublishedVersion(1, now);
        await dbContext.SaveChangesAsync();
        proposal.MarkSent(now);
        proposal.Accept(version.Id, now);
        await dbContext.SaveChangesAsync();
        return new SeededContext(
            eventEntity.Id,
            client.Id,
            clientPerson.Id,
            proposal.Id,
            version.Id);
    }

    private async Task<JsonElement> GetAuthorizedAsync(
        TestSession session,
        string url)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            url,
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> PostAuthorizedAsync(
        TestSession session,
        string url,
        object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed record SeededContext(
        Guid EventId,
        Guid ClientId,
        Guid ClientPersonId,
        Guid ProposalId,
        Guid ProposalVersionId);
}
