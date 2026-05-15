namespace Api.Domain.Observability;

public sealed class PoisonQueueRemediationRun
{
    public Guid Id { get; private set; }
    public string EndpointName { get; private set; } = string.Empty;
    public string JobType { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string RecommendedAction { get; private set; } = string.Empty;
    public string RemediationPath { get; private set; } = string.Empty;
    public string Outcome { get; private set; } = string.Empty;
    public string ExecutedBy { get; private set; } = string.Empty;
    public DateTime ExecutedAtUtc { get; private set; }
    public DateTime? DetectedAtUtc { get; private set; }
    public decimal ResolutionLatencyMinutes { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private PoisonQueueRemediationRun() { }

    public PoisonQueueRemediationRun(
        string endpointName,
        string jobType,
        string severity,
        string recommendedAction,
        string remediationPath,
        string outcome,
        string executedBy,
        DateTime executedAtUtc,
        DateTime? detectedAtUtc,
        string notes)
    {
        Id = Guid.NewGuid();
        EndpointName = endpointName.Trim();
        JobType = jobType.Trim().ToLowerInvariant();
        Severity = severity.Trim().ToLowerInvariant();
        RecommendedAction = recommendedAction.Trim();
        RemediationPath = remediationPath.Trim();
        Outcome = outcome.Trim().ToLowerInvariant();
        ExecutedBy = string.IsNullOrWhiteSpace(executedBy) ? "system" : executedBy.Trim();
        ExecutedAtUtc = executedAtUtc;
        DetectedAtUtc = detectedAtUtc;
        ResolutionLatencyMinutes = detectedAtUtc.HasValue
            ? decimal.Round((decimal)Math.Max(0, (executedAtUtc - detectedAtUtc.Value).TotalMinutes), 2)
            : 0m;
        Notes = notes.Trim();
    }

    public void UpdateOutcome(string outcome, string executedBy, DateTime executedAtUtc, string notes)
    {
        Outcome = outcome.Trim().ToLowerInvariant();
        ExecutedBy = string.IsNullOrWhiteSpace(executedBy) ? "system" : executedBy.Trim();
        ExecutedAtUtc = executedAtUtc;
        ResolutionLatencyMinutes = DetectedAtUtc.HasValue
            ? decimal.Round((decimal)Math.Max(0, (executedAtUtc - DetectedAtUtc.Value).TotalMinutes), 2)
            : 0m;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
    }
}
