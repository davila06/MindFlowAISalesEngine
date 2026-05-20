using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Scoring;

/// <summary>
/// Interface for AI-based lead scoring service integration.
/// </summary>
public interface ILeadScoringAIService
{
    /// <summary>
    /// Gets a predictive score for a lead using an external AI model.
    /// </summary>
    /// <param name="leadId">Lead identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Predicted score and explainability details.</returns>
    Task<LeadAIScoreResult?> PredictScoreAsync(Guid leadId, CancellationToken cancellationToken);
}

public class LeadAIScoreResult
{
    public Guid LeadId { get; set; }
    public int Score { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime ScoredAtUtc { get; set; }
    public List<ScoreContributionItem> Contributions { get; set; } = new();
}

public class ScoreContributionItem
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool Applied { get; set; }
}
