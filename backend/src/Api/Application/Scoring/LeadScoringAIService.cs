using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Application.Scoring;

/// <summary>
/// Implementation of ILeadScoringAIService that calls an external AI microservice.
/// </summary>
public class LeadScoringAIService : ILeadScoringAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _aiServiceBaseUrl;

    public LeadScoringAIService(HttpClient httpClient, string aiServiceBaseUrl)
    {
        _httpClient = httpClient;
        _aiServiceBaseUrl = aiServiceBaseUrl.TrimEnd('/');
    }

    public async Task<LeadAIScoreResult?> PredictScoreAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var url = $"{_aiServiceBaseUrl}/api/score/{leadId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<LeadAIScoreResult>(cancellationToken: cancellationToken);
    }
}
