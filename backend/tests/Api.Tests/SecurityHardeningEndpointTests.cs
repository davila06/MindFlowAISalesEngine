using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests;

public sealed class StrictSecurityTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"strict_security_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:StrictMode"] = "true",
                ["Security:JwtIssuer"] = "novamind-tests",
                ["Security:JwtAudience"] = "novamind-tests-client",
                ["Security:JwtSigningKey"] = "NOVAMIND_TEST_SIGNING_KEY_2026_1234567890",
                ["Security:LeadIntakeApiKey"] = "intake-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LeadsDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    public string BuildToken(string tenantId, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("NOVAMIND_TEST_SIGNING_KEY_2026_1234567890"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("tenant_id", tenantId),
            new Claim("role", role),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "novamind-tests",
            audience: "novamind-tests-client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

public class SecurityHardeningEndpointTests : IClassFixture<StrictSecurityTestFactory>
{
    private readonly StrictSecurityTestFactory _factory;

    public SecurityHardeningEndpointTests(StrictSecurityTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StrictMode_UnauthenticatedWrite_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "rule-sec",
            Trigger = "lead.created",
            Conditions = Array.Empty<object>(),
            Actions = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StrictMode_TenantClaimMismatchHeader_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/overview");
        request.Headers.Add("X-Tenant-Id", "tenant-b");
        request.Headers.Add("X-User-Role", "Admin");
        request.Headers.Add("X-Authenticated-Tenant", "tenant-a");
        request.Headers.Add("X-Authenticated-Role", "Admin");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OperationalSnapshot_NonAdminRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analytics/advanced/metrics/history/snapshot");
        request.Headers.Add("X-Tenant-Id", "tenant-sec");
        request.Headers.Add("X-User-Role", "Sales");
        request.Headers.Add("X-Authenticated-Tenant", "tenant-sec");
        request.Headers.Add("X-Authenticated-Role", "Sales");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApiResponses_ReturnSecurityHeaders()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/overview");
        request.Headers.Add("X-Tenant-Id", "tenant-sec");
        request.Headers.Add("X-User-Role", "Admin");
        request.Headers.Add("X-Authenticated-Tenant", "tenant-sec");
        request.Headers.Add("X-Authenticated-Role", "Admin");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task SmtpSettings_FailedAttempts_AreBlocked()
    {
        using var client = _factory.CreateClient();

        for (var i = 1; i <= 5; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/api/email/smtp-settings")
            {
                Content = JsonContent.Create(new
                {
                    Host = "smtp.test",
                    Port = 0,
                    Username = "user",
                    Password = "secret",
                    FromEmail = "invalid-email",
                    FromName = "Ops",
                    EnableSsl = true
                })
            };
            request.Headers.Add("X-Tenant-Id", "tenant-bruteforce");
            request.Headers.Add("X-User-Role", "Admin");
            request.Headers.Add("X-Authenticated-Tenant", "tenant-bruteforce");
            request.Headers.Add("X-Authenticated-Role", "Admin");

            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var blockedRequest = new HttpRequestMessage(HttpMethod.Put, "/api/email/smtp-settings")
        {
            Content = JsonContent.Create(new
            {
                Host = "smtp.test",
                Port = 0,
                Username = "user",
                Password = "secret",
                FromEmail = "invalid-email",
                FromName = "Ops",
                EnableSsl = true
            })
        };
        blockedRequest.Headers.Add("X-Tenant-Id", "tenant-bruteforce");
        blockedRequest.Headers.Add("X-User-Role", "Admin");
        blockedRequest.Headers.Add("X-Authenticated-Tenant", "tenant-bruteforce");
        blockedRequest.Headers.Add("X-Authenticated-Role", "Admin");

        var blockedResponse = await client.SendAsync(blockedRequest);

        Assert.Equal((HttpStatusCode)429, blockedResponse.StatusCode);
    }

    [Fact]
    public async Task SmtpPassword_IsEncryptedAtRest()
    {
        using var client = _factory.CreateClient();

        var password = "P@ssw0rd_123";
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/email/smtp-settings")
        {
            Content = JsonContent.Create(new
            {
                Host = "smtp.novamind.test",
                Port = 587,
                Username = "ops@novamind.test",
                Password = password,
                FromEmail = "ops@novamind.test",
                FromName = "NovaMind",
                EnableSsl = true
            })
        };
        request.Headers.Add("X-Tenant-Id", "tenant-secret");
        request.Headers.Add("X-User-Role", "Admin");
        request.Headers.Add("X-Authenticated-Tenant", "tenant-secret");
        request.Headers.Add("X-Authenticated-Role", "Admin");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        var stored = await dbContext.SmtpSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(stored);
        Assert.NotEqual(password, stored!.Password);
    }
}
