namespace Api.Domain.Scoring;

public static class LeadScorePriority
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";

    public const int MediumThreshold = 50;
    public const int HighThreshold = 80;
}
