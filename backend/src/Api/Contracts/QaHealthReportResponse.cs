namespace Api.Contracts;

/// <summary>
/// QA-17: Weekly automated quality health report response.
/// Aggregates key quality indicators into a single snapshot.
/// </summary>
public class QaHealthReportResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ReportWindowLabel { get; init; } = string.Empty;
    public int WindowDays { get; init; }

    // ── Lead quality indicators ───────────────────────────────────────────────
    public int TotalLeads { get; init; }
    public int NewLeadsInWindow { get; init; }
    public decimal LeadEmailCompleteness { get; init; }
    public int DuplicateCandidateCount { get; init; }

    // ── Pipeline health ───────────────────────────────────────────────────────
    public int ActiveOpportunities { get; init; }
    public int WonInWindow { get; init; }
    public decimal ConversionRatePercent { get; init; }

    // ── Rules engine health ───────────────────────────────────────────────────
    public int ActiveRules { get; init; }
    public int InactiveRules { get; init; }

    // ── Scoring consistency ───────────────────────────────────────────────────
    public int LeadsWithScoringVersion { get; init; }
    public decimal ScoringCoveragePercent { get; init; }

    // ── Data anomalies ────────────────────────────────────────────────────────
    public int AnomalyEventsInWindow { get; init; }

    // ── Overall quality score (0–100) ─────────────────────────────────────────
    public int QualityScore { get; init; }
    public string QualityGrade { get; init; } = string.Empty;   // A/B/C/D/F
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
