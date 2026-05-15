namespace Api.Application.Companies;

public class CompanyNotFoundException : Exception
{
    public CompanyNotFoundException(Guid companyId)
        : base($"Company with id '{companyId}' was not found.")
    {
    }
}