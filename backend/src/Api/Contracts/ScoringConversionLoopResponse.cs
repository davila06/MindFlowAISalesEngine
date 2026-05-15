namespace Api.Contracts;

public class ScoringConversionLoopResponse
{
    public IReadOnlyList<ScoringConversionBucketItem> Buckets { get; init; } = [];
}

public class ScoringConversionBucketItem
{
    public string Bucket { get; init; } = string.Empty;
    public int Leads { get; init; }
    public int Won { get; init; }
    public decimal ConversionRatePercent { get; init; }
}
