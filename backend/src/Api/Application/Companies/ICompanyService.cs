using Api.Contracts;

namespace Api.Application.Companies;

public interface ICompanyService
{
    Task<CompanyResponse> CreateAsync(CompanyCreateRequest request, CancellationToken cancellationToken);
    Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyResponse>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken);
    Task<CompanyResponse> UpdateAsync(Guid id, CompanyUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}