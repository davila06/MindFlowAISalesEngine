using Api.Application.AnalyticsAdvanced;
using Api.Application.Email;
using Api.Application.Observability;
using Api.Domain.Observability;

namespace Api.Tests;

public sealed class AlertEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAndNotifyAsync_WithinCooldownAndMinorDelta_DeduplicatesAlert()
    {
        var threshold = new AlertThreshold(
            endpointName: "api/test",
            maxErrorRatePercent: 1m,
            maxAverageLatencyMs: 500m,
            notificationEmail: "ops@novamind.local",
            isActive: true);

        var latest = new AlertEvent(
            threshold.Id,
            "api/test",
            "ErrorRatePercent",
            observedValue: 5m,
            thresholdValue: 1m,
            triggeredAtUtc: DateTime.UtcNow.AddMinutes(-5));

        var thresholdRepo = new FakeThresholdRepository(threshold);
        var eventRepo = new FakeAlertEventRepository(latest);
        var emailService = new FakeEmailService();
        var service = new AlertEvaluationService(thresholdRepo, eventRepo, emailService);

        var snapshot = new AnalyticsObservabilitySnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Endpoints =
            [
                new AnalyticsEndpointMetricSnapshot
                {
                    Endpoint = "api/test",
                    RequestCount = 100,
                    ErrorCount = 6,
                    AverageLatencyMs = 100m
                }
            ]
        };

        await service.EvaluateAndNotifyAsync(snapshot, CancellationToken.None);

        Assert.Empty(eventRepo.AddedEvents);
        Assert.Equal(0, emailService.AlertEmailAttempts);
    }

    [Fact]
    public async Task EvaluateAndNotifyAsync_WithinCooldownAndSignificantDelta_CreatesAlert()
    {
        var threshold = new AlertThreshold(
            endpointName: "api/test",
            maxErrorRatePercent: 1m,
            maxAverageLatencyMs: 500m,
            notificationEmail: "ops@novamind.local",
            isActive: true);

        var latest = new AlertEvent(
            threshold.Id,
            "api/test",
            "ErrorRatePercent",
            observedValue: 4m,
            thresholdValue: 1m,
            triggeredAtUtc: DateTime.UtcNow.AddMinutes(-5));

        var thresholdRepo = new FakeThresholdRepository(threshold);
        var eventRepo = new FakeAlertEventRepository(latest);
        var emailService = new FakeEmailService();
        var service = new AlertEvaluationService(thresholdRepo, eventRepo, emailService);

        var snapshot = new AnalyticsObservabilitySnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Endpoints =
            [
                new AnalyticsEndpointMetricSnapshot
                {
                    Endpoint = "api/test",
                    RequestCount = 100,
                    ErrorCount = 8,
                    AverageLatencyMs = 100m
                }
            ]
        };

        await service.EvaluateAndNotifyAsync(snapshot, CancellationToken.None);

        Assert.Single(eventRepo.AddedEvents);
        Assert.Equal(1, emailService.AlertEmailAttempts);
    }

    private sealed class FakeThresholdRepository : IAlertThresholdRepository
    {
        private readonly IReadOnlyList<AlertThreshold> _thresholds;

        public FakeThresholdRepository(params AlertThreshold[] thresholds)
        {
            _thresholds = thresholds;
        }

        public Task AddAsync(AlertThreshold threshold, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AlertThreshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_thresholds.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<AlertThreshold>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(_thresholds);

        public Task<IReadOnlyList<AlertThreshold>> GetActiveAsync(CancellationToken cancellationToken)
            => Task.FromResult(_thresholds.Where(x => x.IsActive).ToList() as IReadOnlyList<AlertThreshold>);

        public Task<int> CountActiveAsync(CancellationToken cancellationToken)
            => Task.FromResult(_thresholds.Count(x => x.IsActive));

        public void Remove(AlertThreshold threshold)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAlertEventRepository : IAlertEventRepository
    {
        private readonly AlertEvent? _latest;

        public FakeAlertEventRepository(AlertEvent? latest)
        {
            _latest = latest;
        }

        public List<AlertEvent> AddedEvents { get; } = [];

        public Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken)
        {
            AddedEvents.Add(alertEvent);
            return Task.CompletedTask;
        }

        public Task<AlertEvent?> GetLatestAsync(string endpointName, string metricName, CancellationToken cancellationToken)
            => Task.FromResult(_latest);

        public Task<AlertEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(AddedEvents.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<AlertEvent>> QueryAsync(
            string? endpointName,
            string? metricName,
            DateTime? startUtc,
            DateTime? endUtc,
            bool? notificationSent,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken)
            => Task.FromResult(AddedEvents.ToList() as IReadOnlyList<AlertEvent>);

        public Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(AddedEvents
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? "open" : x.Status.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count()) as IReadOnlyDictionary<string, int>);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> PurgeAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class FakeEmailService : IEmailService
    {
        public int AlertEmailAttempts { get; private set; }

        public Task SendLeadWelcomeAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SendLeadFollowUpAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> SendProposalAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, decimal amount, string currency, string trackingUrl, byte[] pdfBytes, string pdfFileName, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> SendProposalReminderAsync(Guid leadId, string? toEmail, string recipientName, string proposalTitle, string trackingUrl, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> SendCustomerWelcomeAsync(Guid leadId, string? toEmail, string trackingUrl, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> SendAnalyticsDegradationAlertAsync(string toEmail, string endpointName, string metricName, decimal observedValue, decimal thresholdValue, DateTime triggeredAtUtc, CancellationToken cancellationToken)
        {
            AlertEmailAttempts++;
            return Task.FromResult(true);
        }
    }
}
