namespace Api.Domain.Pipeline;

public class Opportunity
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid StageId { get; private set; }
    public string Title { get; private set; }
    public decimal Value { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string CurrentVersionToken => UpdatedAtUtc.Ticks.ToString();

    private Opportunity()
    {
        Title = string.Empty;
    }

    public Opportunity(Guid leadId, Guid stageId, string title, decimal value)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        StageId = stageId;
        Title = title;
        Value = value;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void MoveToStage(Guid stageId)
    {
        StageId = stageId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}