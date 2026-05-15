using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class LeadIntakeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeadIntakeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostLeadIntake_WithValidPayload_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        var payload = new
        {
            Email = "SALES@Example.COM ",
            Phone = " (555) 123-4567 ",
            Source = "landing-page"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LeadIntakeResponse>();

        Assert.NotNull(body);
        Assert.Equal("sales@example.com", body.Email);
        Assert.Equal("5551234567", body.Phone);
        Assert.Equal("landing-page", body.Source);
    }

    [Fact]
    public async Task PostLeadIntake_WithMissingContactFields_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var payload = new
        {
            Email = " ",
            Phone = " ",
            Source = "landing-page"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostLeadIntake_WithInvalidPhoneForCountry_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var payload = new
        {
            Email = "lead-us@example.com",
            Phone = "12345",
            Source = "landing-page",
            Country = "us"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostLeadIntake_WithValidPhoneForCountry_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        var payload = new
        {
            Email = "lead-us-valid@example.com",
            Phone = "(415) 555-2671",
            Source = "landing-page",
            Country = "us"
        };

        var response = await client.PostAsJsonAsync("/api/leads/intake", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private sealed class LeadIntakeResponse
    {
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string Source { get; init; } = string.Empty;
    }
}
