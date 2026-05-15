using System.Net.Mail;
using Api.Application.Common.Interfaces;
using Api.Contracts;
using Api.Domain.Contacts;
using System.Text.Json;

namespace Api.Application.Contacts;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;

    public ContactService(
        IContactRepository contactRepository,
        ILeadRepository leadRepository,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository)
    {
        _contactRepository = contactRepository;
        _leadRepository = leadRepository;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
    }

    public async Task<ContactResponse> CreateAsync(ContactCreateRequest request, CancellationToken cancellationToken)
    {
        var fullName = NormalizeFullName(request.FullName);
        var email = NormalizeEmail(request.Email);
        var phone = NormalizePhone(request.Phone);

        var errors = Validate(fullName, email, phone, request.LeadId);
        if (errors.Count > 0)
        {
            throw new ContactValidationException(errors);
        }

        if (!await _leadRepository.ExistsAsync(request.LeadId, cancellationToken))
        {
            throw new ContactValidationException(new Dictionary<string, string[]>
            {
                ["leadId"] = ["Lead does not exist."]
            });
        }

        var isDuplicate = await _contactRepository.ExistsByEmailOrPhoneAsync(email, phone, null, cancellationToken);
        if (isDuplicate)
        {
            throw new ContactConflictException("A contact with the same email or phone already exists.");
        }

        var contact = new Contact(request.LeadId, fullName, email, phone);
        await _contactRepository.AddAsync(contact, cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new Api.Domain.Leads.LeadAuditSnapshot(
                contact.LeadId,
                "contact.created",
                "system",
                JsonSerializer.Serialize(new { contact.Id, contact.FullName, contact.Email, contact.Phone })),
            cancellationToken);

        return ToResponse(contact);
    }

    public async Task<IReadOnlyList<ContactResponse>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken)
    {
        var contacts = await _contactRepository.ListAsync(leadId, search, cancellationToken);
        return contacts.Select(ToResponse).ToList();
    }

    public async Task<ContactResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ContactNotFoundException(id);

        return ToResponse(contact);
    }

    public async Task<ContactResponse> UpdateAsync(Guid id, ContactUpdateRequest request, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ContactNotFoundException(id);

        var fullName = NormalizeFullName(request.FullName);
        var email = NormalizeEmail(request.Email);
        var phone = NormalizePhone(request.Phone);

        var errors = Validate(fullName, email, phone, contact.LeadId);
        if (errors.Count > 0)
        {
            throw new ContactValidationException(errors);
        }

        var isDuplicate = await _contactRepository.ExistsByEmailOrPhoneAsync(email, phone, id, cancellationToken);
        if (isDuplicate)
        {
            throw new ContactConflictException("A contact with the same email or phone already exists.");
        }

        contact.Update(fullName, email, phone);
        await _contactRepository.SaveChangesAsync(cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new Api.Domain.Leads.LeadAuditSnapshot(
                contact.LeadId,
                "contact.updated",
                "system",
                JsonSerializer.Serialize(new { contact.Id, contact.FullName, contact.Email, contact.Phone })),
            cancellationToken);

        return ToResponse(contact);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ContactNotFoundException(id);

        await _contactRepository.DeleteAsync(contact, cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new Api.Domain.Leads.LeadAuditSnapshot(
                contact.LeadId,
                "contact.deleted",
                "system",
                JsonSerializer.Serialize(new { contact.Id })),
            cancellationToken);
    }

    private static string? NormalizeFullName(string? fullName)
    {
        return string.IsNullOrWhiteSpace(fullName)
            ? null
            : fullName.Trim().ToLowerInvariant();
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static Dictionary<string, string[]> Validate(string? fullName, string? email, string? phone, Guid leadId)
    {
        var errors = new Dictionary<string, string[]>();

        if (leadId == Guid.Empty)
        {
            errors["leadId"] = ["LeadId is required."];
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors["fullName"] = ["FullName is required."];
        }

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            errors["contact"] = ["At least one contact field is required (email or phone)."];
        }

        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
        {
            errors["email"] = ["Email format is invalid."];
        }

        if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
        {
            errors["phone"] = ["Phone must contain between 8 and 15 digits."];
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhone(string phone)
    {
        return phone.Length is >= 8 and <= 15;
    }

    private static ContactResponse ToResponse(Contact contact)
    {
        return new ContactResponse
        {
            Id = contact.Id,
            LeadId = contact.LeadId,
            FullName = contact.FullName,
            Email = contact.Email,
            Phone = contact.Phone,
            CreatedAtUtc = contact.CreatedAtUtc
        };
    }
}