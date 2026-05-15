namespace Api.Application.Proposals;

public class ProposalValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ProposalValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred for proposal automation.")
    {
        Errors = errors;
    }
}
