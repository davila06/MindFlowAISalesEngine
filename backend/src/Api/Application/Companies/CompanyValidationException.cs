namespace Api.Application.Companies;

public class CompanyValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public CompanyValidationException(Dictionary<string, string[]> errors)
        : base("Company validation failed.")
    {
        Errors = errors;
    }
}