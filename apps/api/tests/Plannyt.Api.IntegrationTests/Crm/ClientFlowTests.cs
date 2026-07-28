using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.Crm;

[Collection(ApiCollection.Name)]
public sealed class ClientFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task CreatePersonClient_WithValidProfile_ReturnsPersistedClient()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "crm-person");
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/clients",
            session.AccessToken,
            JsonContent.Create(new
            {
                clientType = "Person",
                displayName = "Ana Martínez",
                source = "Recomendación",
                person = new
                {
                    firstName = "Ana",
                    lastName = "Martínez",
                    contactEmail = "ana@example.invalid",
                    contactPhone = "+52 899 123 4567",
                    preferredLanguage = "es",
                    timeZone = "America/Matamoros"
                }
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var client = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Person", client.GetProperty("clientType").GetString());
        Assert.Equal("Ana Martínez", client.GetProperty("displayName").GetString());
        Assert.Equal(
            "ana@example.invalid",
            client.GetProperty("person").GetProperty("contactEmail").GetString());
    }

    [Fact]
    public async Task GetClient_FromAnotherTenant_IsForbidden()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "crm-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(factory, "crm-stranger");
        var clientId = await CreateCompanyAsync(owner);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/clients/{clientId}",
            stranger.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddContact_ToCompanyClient_ReturnsTenantContact()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "crm-contact");
        var clientId = await CreateCompanyAsync(owner);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{owner.OrganizationId}/clients/{clientId}/contacts",
            owner.AccessToken,
            JsonContent.Create(new
            {
                firstName = "Luis",
                lastName = "García",
                contactEmail = "luis@example.invalid",
                contactPhone = "+52 899 555 0101",
                preferredLanguage = "es",
                timeZone = "America/Matamoros",
                contactRole = "Compras",
                isPrimary = true
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var contact = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(contact.GetProperty("isPrimary").GetBoolean());
        Assert.Equal("Luis García", contact.GetProperty("displayName").GetString());
    }

    private async Task<Guid> CreateCompanyAsync(TestSession session)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/clients",
            session.AccessToken,
            JsonContent.Create(new
            {
                clientType = "Company",
                displayName = "Corporativo Norte",
                companyName = "Corporativo Norte, S.A. de C.V.",
                source = "Sitio web"
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }
}
