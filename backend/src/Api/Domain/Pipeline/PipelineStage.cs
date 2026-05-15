namespace Api.Domain.Pipeline;

public class PipelineStage
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int Order { get; private set; }
    public string? Color { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PipelineStage()
    {
        Name = string.Empty;
    }

    public PipelineStage(Guid id, string name, int order, string? color)
    {
        Id = id;
        Name = name;
        Order = order;
        Color = color;
        CreatedAtUtc = DateTime.UtcNow;
    }
}