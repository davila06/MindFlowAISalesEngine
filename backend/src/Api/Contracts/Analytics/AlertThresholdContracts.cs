using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Analytics;

public sealed class AlertThresholdCreateRequest
{
    [Required]
    [MaxLength(100)]
    public string EndpointName { get; init; } = string.Empty;

    [Range(0, 100)]
    public decimal MaxErrorRatePercent { get; init; }

    [Range(0, 1000000)]
    public decimal MaxAverageLatencyMs { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string NotificationEmail { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    [MaxLength(500)]
    public string? WebhookUrl { get; init; }
}

public sealed class AlertThresholdUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string EndpointName { get; init; } = string.Empty;

    [Range(0, 100)]
    public decimal MaxErrorRatePercent { get; init; }

    [Range(0, 1000000)]
    public decimal MaxAverageLatencyMs { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string NotificationEmail { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    [MaxLength(500)]
    public string? WebhookUrl { get; init; }
}

public sealed class AlertThresholdResponse
{
    public Guid Id { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public decimal MaxErrorRatePercent { get; init; }
    public decimal MaxAverageLatencyMs { get; init; }
    public string NotificationEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? WebhookUrl { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class AlertThresholdListResponse
{
    public IReadOnlyList<AlertThresholdResponse> Items { get; init; } = [];
}
