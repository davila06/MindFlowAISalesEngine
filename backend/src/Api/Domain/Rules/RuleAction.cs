namespace Api.Domain.Rules;

public class RuleAction
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public string Type { get; private set; }
    public string Value { get; private set; }

    private RuleAction()
    {
        Type = string.Empty;
        Value = string.Empty;
    }

    public RuleAction(string type, string value)
    {
        Id = Guid.NewGuid();
        Type = type;
        Value = value;
    }

    public void Update(string type, string value)
    {
        Type = type;
        Value = value;
    }
}
