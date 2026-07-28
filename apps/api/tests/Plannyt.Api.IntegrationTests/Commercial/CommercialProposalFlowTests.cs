using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.Commercial;

[Collection(ApiCollection.Name)]
public sealed class CommercialProposalFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task ProspectToAcceptedProposalAndPreliminaryEvent_CompletesVersionedFlow()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "commercial-flow");
        var prospectId = await CreateOpportunityAsync(session);
        var serviceId = await CreateServiceAsync(session);
        var packageId = await CreatePackageAsync(session, serviceId);
        var couponId = await CreateCouponAsync(session);
        var proposalId = await CreateProposalAsync(
            session,
            prospectId,
            serviceId,
            packageId,
            couponId,
            12500m);

        var versionOne = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals/{proposalId}/publish",
            new { });
        Assert.Equal(1, versionOne.GetProperty("versionNumber").GetInt32());
        Assert.Equal(18792m, versionOne
            .GetProperty("totals")
            .GetProperty("grandTotal")
            .GetDecimal());

        var firstToken = await SendAndGetTokenAsync(session, proposalId);
        var publicVersionOne = await GetPublicAsync(firstToken);
        Assert.Equal(1, publicVersionOne.GetProperty("versionNumber").GetInt32());
        Assert.False(publicVersionOne.TryGetProperty("internalNotes", out _));
        Assert.False(publicVersionOne.TryGetProperty("couponId", out _));
        using (var pdf = await factory.CreateClient().GetAsync(
            $"/api/public/proposals/{firstToken}/pdf"))
        {
            pdf.EnsureSuccessStatusCode();
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            var bytes = await pdf.Content.ReadAsByteArrayAsync();
            Assert.StartsWith("%PDF-1.4", System.Text.Encoding.ASCII.GetString(bytes));
        }

        using (var publicComment = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/proposals/{firstToken}/comments",
            new
            {
                authorDisplayName = "María Hernández",
                content = "¿Podemos ajustar la coordinación?",
                proposalLineId = (Guid?)null,
                parentCommentId = (Guid?)null
            }))
        {
            Assert.Equal(HttpStatusCode.Created, publicComment.StatusCode);
        }

        using (var changes = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/proposals/{firstToken}/request-changes",
            new
            {
                authorDisplayName = "María Hernández",
                reason = "Cambiar el alcance de coordinación."
            }))
        {
            changes.EnsureSuccessStatusCode();
        }

        await UpdateProposalAsync(
            session,
            proposalId,
            prospectId,
            serviceId,
            packageId,
            couponId,
            13500m);
        using (var staleSendRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/proposals/{proposalId}/send",
            session.AccessToken,
            JsonContent.Create(new { expiresAt = (DateTimeOffset?)null })))
        using (var staleSendResponse = await factory.CreateClient().SendAsync(
            staleSendRequest))
        {
            Assert.Equal(HttpStatusCode.Conflict, staleSendResponse.StatusCode);
        }

        var versionTwo = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals/{proposalId}/publish",
            new { });
        Assert.Equal(2, versionTwo.GetProperty("versionNumber").GetInt32());
        var secondToken = await SendAndGetTokenAsync(session, proposalId);

        using (var superseded = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/proposals/{firstToken}/accept",
            new { authorDisplayName = "María Hernández", reason = (string?)null }))
        {
            Assert.Equal(HttpStatusCode.Gone, superseded.StatusCode);
        }

        using (var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/proposals/{secondToken}/accept",
            new
            {
                authorDisplayName = "María Hernández",
                reason = "Acepto la versión ajustada."
            }))
        {
            acceptance.EnsureSuccessStatusCode();
            var accepted = await acceptance.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
            Assert.Equal(2, accepted.GetProperty("versionNumber").GetInt32());
        }

        var conversion = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/prospects/{prospectId}/convert",
            new
            {
                existingClientId = (Guid?)null,
                newClientType = "Person",
                confirmCreateDespiteMatches = true
            });
        Assert.True(conversion.GetProperty("createdNewClient").GetBoolean());
        var clientId = conversion.GetProperty("clientId").GetGuid();

        var preliminary = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/prospects/{prospectId}/preliminary-event",
            new
            {
                existingEventId = (Guid?)null,
                name = "Boda de María y Carlos",
                eventType = "Wedding",
                startDateTime = DateTimeOffset.UtcNow.AddMonths(6),
                timeZone = "America/Matamoros",
                city = "Matamoros",
                countryCode = "MX",
                estimatedGuestCount = 140
            });
        var eventId = preliminary.GetProperty("eventId").GetGuid();

        var prospect = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/prospects/{prospectId}");
        Assert.Equal("Won", prospect.GetProperty("status").GetString());
        Assert.Equal(clientId, prospect.GetProperty("convertedClientId").GetGuid());

        var eventClients = await GetAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/clients");
        Assert.Contains(
            eventClients.EnumerateArray(),
            item => item.GetProperty("clientId").GetGuid() == clientId);
    }

    [Fact]
    public async Task Proposal_WithProspectFromAnotherTenant_IsRejected()
    {
        var tenantA = await TestSessionFactory.RegisterPlannerAsync(factory, "proposal-tenant-a");
        var tenantB = await TestSessionFactory.RegisterPlannerAsync(factory, "proposal-tenant-b");
        var prospectB = await CreateOpportunityAsync(tenantB);
        var serviceA = await CreateServiceAsync(tenantA);

        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{tenantA.OrganizationId}/proposals",
            tenantA.AccessToken,
            JsonContent.Create(CreateProposalRequest(
                prospectB,
                serviceA,
                null,
                null,
                5000m)));
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicToken_OnlyReturnsItsOwnProposal()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "proposal-token");
        var serviceId = await CreateServiceAsync(session);
        var firstProspect = await CreateOpportunityAsync(session);
        var secondProspect = await CreateOpportunityAsync(session);
        var firstProposal = await CreateProposalAsync(
            session,
            firstProspect,
            serviceId,
            null,
            null,
            5000m);
        var secondProposal = await CreateProposalAsync(
            session,
            secondProspect,
            serviceId,
            null,
            null,
            9000m);
        await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals/{firstProposal}/publish",
            new { });
        await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals/{secondProposal}/publish",
            new { });
        var token = await SendAndGetTokenAsync(session, firstProposal);

        var shared = await GetPublicAsync(token);

        Assert.Equal(firstProposal, shared.GetProperty("proposalId").GetGuid());
        Assert.NotEqual(secondProposal, shared.GetProperty("proposalId").GetGuid());
        Assert.Equal(2, shared.GetProperty("lines").GetArrayLength());
    }

    private async Task<Guid> CreateOpportunityAsync(TestSession session)
    {
        var prospect = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/prospects",
            new
            {
                displayName = "María Hernández",
                firstName = "María",
                lastName = "Hernández",
                companyName = (string?)null,
                email = $"maria-{Guid.NewGuid():N}@example.invalid",
                phone = $"+52899{Random.Shared.Next(1000000, 9999999)}",
                source = "Instagram",
                eventTypeInterest = "Wedding",
                estimatedEventDate = new DateOnly(2027, 2, 14),
                estimatedGuestCount = 140,
                estimatedBudget = 180000m,
                currencyCode = "MXN",
                city = "Matamoros",
                notes = "Prefiere contacto por WhatsApp",
                assignedUserId = session.UserAccountId
            });
        var prospectId = prospect.GetProperty("id").GetGuid();
        await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/prospects/{prospectId}/activities",
            new
            {
                activityType = "FollowUp",
                subject = "Enviar opciones iniciales",
                description = "Compartir catálogo de producción.",
                scheduledAt = DateTimeOffset.UtcNow.AddDays(1),
                completedAt = (DateTimeOffset?)null,
                assignedUserId = session.UserAccountId,
                visibility = "Internal"
            });
        foreach (var status in new[] { "Contacted", "Qualified", "Opportunity" })
        {
            await PostAuthorizedAsync(
                session,
                $"/api/organizations/{session.OrganizationId}/prospects/{prospectId}/status",
                new { newStatus = status, reason = (string?)null });
        }

        return prospectId;
    }

    private async Task<Guid> CreateServiceAsync(TestSession session)
    {
        var service = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/catalog/services",
            new
            {
                name = $"Producción integral {Guid.NewGuid():N}",
                description = "Planeación y coordinación del evento.",
                category = "Producción",
                pricingType = "Fixed",
                basePrice = 12500m,
                currencyCode = "MXN",
                taxBehavior = "Exclusive",
                isNegotiable = true,
                isActive = true,
                sortOrder = 1
            });
        return service.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreatePackageAsync(
        TestSession session,
        Guid serviceId)
    {
        var package = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/catalog/packages",
            new
            {
                name = $"Celebración esencial {Guid.NewGuid():N}",
                description = "Paquete base.",
                basePrice = 6000m,
                currencyCode = "MXN",
                isNegotiable = false,
                isActive = true,
                items = new[]
                {
                    new
                    {
                        serviceCatalogItemId = serviceId,
                        quantity = 1m,
                        isOptional = false,
                        includedPrice = (decimal?)6000m,
                        sortOrder = 0
                    }
                }
            });
        return package.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateCouponAsync(TestSession session)
    {
        var coupon = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/catalog/coupons",
            new
            {
                code = $"BIENVENIDA{Random.Shared.Next(1000, 9999)}",
                description = "Beneficio inicial",
                discountType = "Percentage",
                discountValue = 10m,
                startsAt = DateTimeOffset.UtcNow.AddDays(-1),
                endsAt = DateTimeOffset.UtcNow.AddDays(30),
                maximumUses = 10,
                isActive = true
            });
        return coupon.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateProposalAsync(
        TestSession session,
        Guid prospectId,
        Guid serviceId,
        Guid? packageId,
        Guid? couponId,
        decimal unitPrice)
    {
        var proposal = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals",
            CreateProposalRequest(
                prospectId,
                serviceId,
                packageId,
                couponId,
                unitPrice));
        return proposal.GetProperty("id").GetGuid();
    }

    private async Task UpdateProposalAsync(
        TestSession session,
        Guid proposalId,
        Guid prospectId,
        Guid serviceId,
        Guid packageId,
        Guid couponId,
        decimal unitPrice)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/organizations/{session.OrganizationId}/proposals/{proposalId}/draft",
            session.AccessToken,
            JsonContent.Create(CreateProposalRequest(
                prospectId,
                serviceId,
                packageId,
                couponId,
                unitPrice)));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static object CreateProposalRequest(
        Guid prospectId,
        Guid serviceId,
        Guid? packageId,
        Guid? couponId,
        decimal unitPrice) =>
        new
        {
            prospectId,
            clientId = (Guid?)null,
            eventId = (Guid?)null,
            currencyCode = "MXN",
            validUntil = DateTimeOffset.UtcNow.AddDays(14),
            sharedIntroduction = "Una propuesta preparada para tu evento.",
            sharedTerms = "Vigencia de catorce días.",
            internalNotes = "Margen revisado por el equipo comercial.",
            generalDiscountType = "FixedAmount",
            generalDiscountValue = 500m,
            couponId,
            lines = new object[]
            {
                new
                {
                    description = "Producción integral",
                    serviceCatalogItemId = serviceId,
                    packageId = (Guid?)null,
                    quantity = 1m,
                    unitPrice,
                    discountType = "None",
                    discountValue = 0m,
                    taxRate = 16m,
                    isOptional = false,
                    sortOrder = 0
                },
                new
                {
                    description = "Coordinación adicional",
                    serviceCatalogItemId = (Guid?)null,
                    packageId,
                    quantity = 1m,
                    unitPrice = 6000m,
                    discountType = "None",
                    discountValue = 0m,
                    taxRate = 16m,
                    isOptional = false,
                    sortOrder = 1
                }
            }
        };

    private async Task<string> SendAndGetTokenAsync(
        TestSession session,
        Guid proposalId)
    {
        var link = await PostAuthorizedAsync(
            session,
            $"/api/organizations/{session.OrganizationId}/proposals/{proposalId}/send",
            new { expiresAt = (DateTimeOffset?)null });
        var shareUrl = link.GetProperty("shareUrl").GetString()
            ?? throw new InvalidOperationException("No se generó el enlace.");
        return shareUrl[(shareUrl.LastIndexOf('/') + 1)..];
    }

    private async Task<JsonElement> GetPublicAsync(string token)
    {
        using var response = await factory.CreateClient().GetAsync(
            $"/api/public/proposals/{token}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
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
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            url,
            session.AccessToken,
            JsonContent.Create(body));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
