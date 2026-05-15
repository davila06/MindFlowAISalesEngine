namespace Api.Infrastructure.Security;

public sealed class SecurityRuntimeOptions
{
    public bool StrictMode { get; set; }
    public string JwtIssuer { get; set; } = "novamind-api";
    public string JwtAudience { get; set; } = "novamind-clients";
    public string JwtSigningKey { get; set; } = "NOVAMIND_DEV_ONLY_SUPER_SECRET_KEY_2026";
    public string LeadIntakeApiKey { get; set; } = "";
    public string[] AllowedCorsOrigins { get; set; } =
    [
        "https://app.novamind.local",
        "http://localhost:3000",
        "http://127.0.0.1:3000"
    ];
}
