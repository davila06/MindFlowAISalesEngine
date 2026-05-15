namespace Api.Contracts;

public sealed class SmtpSettingsResponse
{
    public Guid Id { get; init; }
    public string ProviderType { get; init; } = string.Empty;
    public string? ProviderBaseUrl { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string? FromName { get; init; }
    public bool EnableSsl { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
