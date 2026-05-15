using Api.Application.Common.Interfaces;
using Api.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class CompanyRepository : ICompanyRepository
{
    private readonly LeadsDbContext _dbContext;

    public CompanyRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        await _dbContext.Companies.AddAsync(company, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Company?> GetByLeadIdAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return _dbContext.Companies.FirstOrDefaultAsync(x => x.LeadId == leadId, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Companies.AsQueryable();

        if (leadId.HasValue)
        {
            query = query.Where(x => x.LeadId == leadId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.Contains(normalized)
                || (x.Industry != null && x.Industry.Contains(normalized))
                || (x.Website != null && x.Website.Contains(normalized)));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string normalizedName, Guid? ignoreCompanyId, CancellationToken cancellationToken)
    {
        return _dbContext.Companies.AnyAsync(x =>
                (ignoreCompanyId == null || x.Id != ignoreCompanyId.Value)
                && x.Name == normalizedName,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Company company, CancellationToken cancellationToken)
    {
        company.MarkDeleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}