namespace Api.Domain.Email;

public class SmtpSettings
{
    public const string SmtpProviderType = "smtp";
    public const string WebhookProviderType = "webhook";

    public Guid Id { get; private set; }
    public string ProviderType { get; private set; } = SmtpProviderType;
    public string? ProviderBaseUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public string FromEmail { get; private set; } = string.Empty;
    public string? FromName { get; private set; }
    public bool EnableSsl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private SmtpSettings() { }

    public SmtpSettings(string providerType, string? providerBaseUrl, string? apiKey,
        string host, int port, string username, string password,
        string fromEmail, string? fromName, bool enableSsl)
    {
        Id = Guid.NewGuid();
        ProviderType = NormalizeProviderType(providerType);
        ProviderBaseUrl = NormalizeUrl(providerBaseUrl);
        ApiKey = apiKey;
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        FromEmail = fromEmail;
        FromName = fromName;
        EnableSsl = enableSsl;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string providerType, string? providerBaseUrl, string? apiKey,
        string host, int port, string username, string password,
        string fromEmail, string? fromName, bool enableSsl)
    {
        ProviderType = NormalizeProviderType(providerType);
        ProviderBaseUrl = NormalizeUrl(providerBaseUrl);
        ApiKey = apiKey;
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        FromEmail = fromEmail;
        FromName = fromName;
        EnableSsl = enableSsl;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRuntimePassword(string decryptedPassword)
    {
        Password = decryptedPassword;
    }

    public void SetRuntimeApiKey(string? decryptedApiKey)
    {
        ApiKey = decryptedApiKey;
    }

    private static string NormalizeProviderType(string providerType)
    {
        return string.Equals(providerType, WebhookProviderType, StringComparison.OrdinalIgnoreCase)
            ? WebhookProviderType
            : SmtpProviderType;
    }

    private static string? NormalizeUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
