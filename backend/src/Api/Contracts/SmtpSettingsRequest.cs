using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class SmtpSettingsRequest : IValidatableObject
{
    [Required, MaxLength(20)]
    public string ProviderType { get; init; } = "smtp";

    [MaxLength(500)]
    public string? ProviderBaseUrl { get; init; }

    [MaxLength(500)]
    public string? ApiKey { get; init; }

    [MaxLength(253)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    [MaxLength(320)]
    public string Username { get; init; } = string.Empty;

    [MaxLength(500)]
    public string Password { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string FromEmail { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? FromName { get; init; }

    public bool EnableSsl { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var provider = (ProviderType ?? string.Empty).Trim().ToLowerInvariant();
        if (provider is not ("smtp" or "webhook"))
        {
            yield return new ValidationResult(
                "ProviderType must be either 'smtp' or 'webhook'.",
                [nameof(ProviderType)]);
            yield break;
        }

        if (provider == "smtp")
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                yield return new ValidationResult("Host is required for smtp provider.", [nameof(Host)]);
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                yield return new ValidationResult("Username is required for smtp provider.", [nameof(Username)]);
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult("Password is required for smtp provider.", [nameof(Password)]);
            }
        }
        else if (string.IsNullOrWhiteSpace(ProviderBaseUrl))
        {
            yield return new ValidationResult(
                "ProviderBaseUrl is required for webhook provider.",
                [nameof(ProviderBaseUrl)]);
        }
    }
}
