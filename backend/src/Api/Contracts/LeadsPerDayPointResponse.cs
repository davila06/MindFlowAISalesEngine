namespace Api.Contracts;

public class LeadsPerDayPointResponse
{
    public string Date { get; init; } = string.Empty;
    public int Count { get; init; }
}
