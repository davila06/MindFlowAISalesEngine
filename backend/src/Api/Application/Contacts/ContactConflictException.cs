namespace Api.Application.Contacts;

public class ContactConflictException : Exception
{
    public ContactConflictException(string message)
        : base(message)
    {
    }
}