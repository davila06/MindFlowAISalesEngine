namespace Api.Application.Leads;

public static class LeadSourceCatalog
{
    public const string Referral = "referral";
    public const string Web = "web";
    public const string Ads = "ads";
    public const string Event = "event";
    public const string Partner = "partner";
    public const string Other = "other";

    public static readonly HashSet<string> Allowed =
    [
        Referral,
        Web,
        Ads,
        Event,
        Partner,
        Other
    ];

    public static string Normalize(string source)
    {
        var normalized = source.Trim().ToLowerInvariant();
        return Allowed.Contains(normalized) ? normalized : normalized;
    }
}
