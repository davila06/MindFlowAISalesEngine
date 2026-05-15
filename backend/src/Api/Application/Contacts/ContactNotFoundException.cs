namespace Api.Application.Contacts;

public class ContactNotFoundException : Exception
{
    public ContactNotFoundException(Guid contactId)
        : base($"Contact with id '{contactId}' was not found.")
    {
    }
}