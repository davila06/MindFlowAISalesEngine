namespace Api.Application.Contacts;

public class ContactValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ContactValidationException(Dictionary<string, string[]> errors)
        : base("Contact validation failed.")
    {
        Errors = errors;
    }
}