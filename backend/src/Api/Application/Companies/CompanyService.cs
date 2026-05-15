using Api.Application.Common.Interfaces;
using Api.Contracts;
using Api.Domain.Companies;
using Api.Domain.Leads;
using System.Text.Json;

namespace Api.Application.Companies;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;

    public CompanyService(
        ICompanyRepository companyRepository,
        ILeadRepository leadRepository,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository)
    {
        _companyRepository = companyRepository;
        _leadRepository = leadRepository;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
    }

    public async Task<CompanyResponse> CreateAsync(CompanyCreateRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(request.Name);
        var normalizedIndustry = NormalizeIndustry(request.Industry);
        var normalizedWebsite = NormalizeWebsite(request.Website);

        var errors = Validate(normalizedName, normalizedWebsite, request.LeadId);
        if (errors.Count > 0)
        {
            throw new CompanyValidationException(errors);
        }

        if (!await _leadRepository.ExistsAsync(request.LeadId, cancellationToken))
        {
            throw new CompanyValidationException(new Dictionary<string, string[]>
            {
                ["leadId"] = ["Lead does not exist."]
            });
        }

        var isDuplicate = await _companyRepository.ExistsByNameAsync(normalizedName!, null, cancellationToken);
        if (isDuplicate)
        {
            throw new CompanyConflictException("A company with the same name already exists.");
        }

        var company = new Company(request.LeadId, normalizedName!, normalizedWebsite, normalizedIndustry);
        await _companyRepository.AddAsync(company, cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                company.LeadId,
                "company.created",
                "system",
                JsonSerializer.Serialize(new { company.Id, company.Name, company.Industry, company.Website })),
            cancellationToken);

        return ToResponse(company);
    }

    public async Task<IReadOnlyList<CompanyResponse>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken)
    {
        var companies = await _companyRepository.ListAsync(leadId, search, cancellationToken);
        return companies.Select(ToResponse).ToList();
    }

    public async Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CompanyNotFoundException(id);

        return ToResponse(company);
    }

    public async Task<CompanyResponse> UpdateAsync(Guid id, CompanyUpdateRequest request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CompanyNotFoundException(id);

        var normalizedName = NormalizeName(request.Name);
        var normalizedIndustry = NormalizeIndustry(request.Industry);
        var normalizedWebsite = NormalizeWebsite(request.Website);

        var errors = Validate(normalizedName, normalizedWebsite, company.LeadId);
        if (errors.Count > 0)
        {
            throw new CompanyValidationException(errors);
        }

        var isDuplicate = await _companyRepository.ExistsByNameAsync(normalizedName!, id, cancellationToken);
        if (isDuplicate)
        {
            throw new CompanyConflictException("A company with the same name already exists.");
        }

        company.Update(normalizedName!, normalizedWebsite, normalizedIndustry);
        await _companyRepository.SaveChangesAsync(cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                company.LeadId,
                "company.updated",
                "system",
                JsonSerializer.Serialize(new { company.Id, company.Name, company.Industry, company.Website })),
            cancellationToken);

        return ToResponse(company);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CompanyNotFoundException(id);

        await _companyRepository.DeleteAsync(company, cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                company.LeadId,
                "company.deleted",
                "system",
                JsonSerializer.Serialize(new { company.Id })),
            cancellationToken);
    }

    private static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim().ToLowerInvariant();
    }

    private static string? NormalizeWebsite(string? website)
    {
        return string.IsNullOrWhiteSpace(website)
            ? null
            : website.Trim().ToLowerInvariant();
    }

    private static string NormalizeIndustry(string? industry)
    {
        return string.IsNullOrWhiteSpace(industry)
            ? "unknown"
            : industry.Trim().ToLowerInvariant();
    }

    private static Dictionary<string, string[]> Validate(string? name, string? website, Guid leadId)
    {
        var errors = new Dictionary<string, string[]>();

        if (leadId == Guid.Empty)
        {
            errors["leadId"] = ["LeadId is required."];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Company name is required."];
        }

        if (!string.IsNullOrWhiteSpace(website)
            && (!Uri.TryCreate(website, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            errors["website"] = ["Website must be a valid absolute URL."];
        }

        return errors;
    }

    private static CompanyResponse ToResponse(Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            LeadId = company.LeadId,
            Name = company.Name,
            Industry = company.Industry,
            Website = company.Website,
            CreatedAtUtc = company.CreatedAtUtc
        };
    }
}