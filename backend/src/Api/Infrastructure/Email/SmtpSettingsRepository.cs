using Api.Application.Email;
using Api.Domain.Email;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Email;

public class SmtpSettingsRepository : ISmtpSettingsRepository
{
    private readonly LeadsDbContext _context;
    private readonly IDataProtector _protector;

    public SmtpSettingsRepository(
        LeadsDbContext context,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector("smtp-settings-password-v1");
    }

    public async Task<SmtpSettings?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.SmtpSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return null;
        }

        settings.SetRuntimePassword(Decrypt(settings.Password));
        settings.SetRuntimeApiKey(DecryptNullable(settings.ApiKey));
        return settings;
    }

    public async Task UpsertAsync(SmtpSettings settings, CancellationToken cancellationToken)
    {
        var existing = await _context.SmtpSettings
            .Where(s => s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            var secured = new SmtpSettings(
                settings.ProviderType,
                settings.ProviderBaseUrl,
                EncryptNullable(settings.ApiKey),
                settings.Host,
                settings.Port,
                settings.Username,
                Encrypt(settings.Password),
                settings.FromEmail,
                settings.FromName,
                settings.EnableSsl);

            _context.SmtpSettings.Add(secured);
        }
        else
        {
            existing.Update(
                settings.ProviderType,
                settings.ProviderBaseUrl,
                EncryptNullable(settings.ApiKey),
                settings.Host,
                settings.Port,
                settings.Username,
                Encrypt(settings.Password),
                settings.FromEmail,
                settings.FromName,
                settings.EnableSsl);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    private string Decrypt(string cipherText)
    {
        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch
        {
            return cipherText;
        }
    }

    private string? EncryptNullable(string? plainText)
    {
        return string.IsNullOrWhiteSpace(plainText) ? null : Encrypt(plainText);
    }

    private string? DecryptNullable(string? cipherText)
    {
        return string.IsNullOrWhiteSpace(cipherText) ? null : Decrypt(cipherText);
    }
}
