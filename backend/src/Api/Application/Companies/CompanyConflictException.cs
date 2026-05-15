namespace Api.Application.Companies;

public class CompanyConflictException : Exception
{
    public CompanyConflictException(string message)
        : base(message)
    {
    }
}