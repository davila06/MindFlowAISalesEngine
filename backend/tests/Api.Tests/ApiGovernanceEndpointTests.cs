using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class ApiGovernanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiGovernanceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiRequest_WithUnsupportedVersion_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/rules");
        request.Headers.Add("X-Api-Version", "2");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LeadIntake_WithIdempotencyKey_ReplaysCreatedResource()
    {
        using var client = _factory.CreateClient();

        var payload = new
        {
            Email = "idempotency@example.com",
            Phone = "5551112222",
            Source = "api"
        };

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", "lead-intake-001");

        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<LeadIntakeResponse>();

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(payload)
        };
        secondRequest.Headers.Add("Idempotency-Key", "lead-intake-001");

        var secondResponse = await client.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        Assert.True(secondResponse.Headers.TryGetValues("X-Idempotent-Replay", out _));

        var secondBody = await secondResponse.Content.ReadFromJsonAsync<LeadIntakeResponse>();

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.Equal(firstBody.Id, secondBody.Id);
    }

    private sealed class LeadIntakeResponse
    {
        public Guid Id { get; init; }
    }
}
