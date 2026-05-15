namespace Api.Domain.Rules;

public class RuleCondition
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public string Field { get; private set; }
    public string Operator { get; private set; }
    public string Value { get; private set; }

    private RuleCondition()
    {
        Field = string.Empty;
        Operator = string.Empty;
        Value = string.Empty;
    }

    public RuleCondition(string field, string @operator, string value)
    {
        Id = Guid.NewGuid();
        Field = field;
        Operator = @operator;
        Value = value;
    }

    public void Update(string field, string @operator, string value)
    {
        Field = field;
        Operator = @operator;
        Value = value;
    }
}
