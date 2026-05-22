namespace Api.Contracts;

public class LeadIntakeRequest
{
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Source { get; init; }
    public string? Channel { get; init; }
    public string? Campaign { get; init; }
    public string? Country { get; init; }
    public string? ServiceInterest { get; init; }
}
