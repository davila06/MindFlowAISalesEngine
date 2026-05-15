using Api.Domain.Companies;

namespace Api.Application.Common.Interfaces;

public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Company?> GetByLeadIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Company>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string normalizedName, Guid? ignoreCompanyId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Company company, CancellationToken cancellationToken);
}