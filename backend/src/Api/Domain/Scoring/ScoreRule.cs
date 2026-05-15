namespace Api.Domain.Scoring;

public class ScoreRule
{
    public string Key { get; }
    public string Description { get; }
    public int Points { get; }

    public ScoreRule(string key, string description, int points)
    {
        Key = key;
        Description = description;
        Points = points;
    }
}
