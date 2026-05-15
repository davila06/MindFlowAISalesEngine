namespace Api.Application.Leads;

public class LeadIntakeValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public LeadIntakeValidationException(IDictionary<string, string[]> errors)
        : base("Lead intake payload is invalid.")
    {
        Errors = errors;
    }
}
