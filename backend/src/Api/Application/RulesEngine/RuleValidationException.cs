namespace Api.Application.RulesEngine;

public class RuleValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public RuleValidationException(Dictionary<string, string[]> errors)
    {
        Errors = errors;
    }
}
