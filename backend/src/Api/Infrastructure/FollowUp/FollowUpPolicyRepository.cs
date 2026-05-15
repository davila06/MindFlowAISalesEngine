using Api.Application.FollowUp;
using Api.Domain.FollowUp;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.FollowUp;

public sealed class FollowUpPolicyRepository : IFollowUpPolicyRepository
{
    private readonly LeadsDbContext _context;

    public FollowUpPolicyRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task<FollowUpPolicySettings?> GetAsync(CancellationToken cancellationToken)
    {
        return await _context.FollowUpPolicySettings
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(FollowUpPolicySettings settings, CancellationToken cancellationToken)
    {
        var current = await GetAsync(cancellationToken);
        if (current is null)
        {
            _context.FollowUpPolicySettings.Add(settings);
        }
        else
        {
            current.Update(
                settings.QuietHoursEnabled,
                settings.QuietHoursStartHourUtc,
                settings.QuietHoursEndHourUtc,
                settings.GetRules());
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}