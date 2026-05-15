using Api.Contracts;
using Api.Application.Common.Interfaces;
using Api.Domain.Assignment;
using Microsoft.Extensions.Logging;

namespace Api.Application.Assignment;

public class LeadAssignmentService : ILeadAssignmentService
{
    private const string RoundRobinStrategy = "round_robin";
    private const string RuleBasedStrategy = "rule_based";
    private const string ManualStrategy = "manual";
    private const string RebalanceStrategy = "rebalance_availability";

    private readonly IAssignmentUserRepository _userRepository;
    private readonly ILeadAssignmentRepository _assignmentRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAssignmentProtectionStore _assignmentProtectionStore;
    private readonly ILogger<LeadAssignmentService> _logger;

    public LeadAssignmentService(
        IAssignmentUserRepository userRepository,
        ILeadAssignmentRepository assignmentRepository,
        ILeadRepository leadRepository,
        ICompanyRepository companyRepository,
        IAssignmentProtectionStore assignmentProtectionStore,
        ILogger<LeadAssignmentService> logger)
    {
        _userRepository = userRepository;
        _assignmentRepository = assignmentRepository;
        _leadRepository = leadRepository;
        _companyRepository = companyRepository;
        _assignmentProtectionStore = assignmentProtectionStore;
        _logger = logger;
    }

    public async Task<AssignmentUserResponse> CreateUserAsync(
        AssignmentUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = NormalizeName(request.FullName);
        var email = NormalizeEmail(request.Email);

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new AssignmentConflictException($"Assignment user with email '{email}' already exists.");
        }

        var user = new AssignmentUser(
            fullName,
            email,
            request.IsActive,
            request.PreferredCountry,
            request.PreferredIndustry,
            request.MaxActiveLeads,
            request.MinScoreToAssign);
        await _userRepository.AddAsync(user, cancellationToken);

        return MapUser(user);
    }

    public async Task<AssignmentUserResponse?> UpdateUserAvailabilityAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.SetAvailability(isActive);
        await _userRepository.SaveChangesAsync(cancellationToken);

        if (!isActive)
        {
            await RebalanceUnavailableUserAssignmentsAsync(userId, cancellationToken);
        }

        return MapUser(user);
    }

    public async Task<IReadOnlyList<AssignmentUserResponse>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task<LeadAssignmentResponse?> AssignLeadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        if (_assignmentProtectionStore.IsManualProtected(leadId))
        {
            var latestProtected = await _assignmentRepository.GetLatestByLeadIdAsync(leadId, cancellationToken);
            return latestProtected is null ? null : MapAssignment(latestProtected);
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var company = await _companyRepository.GetByLeadIdAsync(leadId, cancellationToken);
        var activeUsers = await _userRepository.GetActiveAsync(cancellationToken);
        if (activeUsers.Count == 0)
        {
            _logger.LogWarning("No active assignment users available. Lead {LeadId} remains unassigned.", leadId);
            return null;
        }

        var allAssignments = await _assignmentRepository.GetAllAsync(cancellationToken);
        var currentLoadByUser = BuildCurrentLoadByUser(allAssignments);

        var usersBelowCapacity = activeUsers
            .Where(user => currentLoadByUser.TryGetValue(user.Id, out var load) ? load < user.MaxActiveLeads : true)
            .ToList();

        var capacityPool = usersBelowCapacity.Count > 0 ? usersBelowCapacity : activeUsers.ToList();

        var ruleCandidates = capacityPool
            .Where(HasExplicitAssignmentRules)
            .Where(user => MatchesAssignmentRules(user, lead.Country, company?.Industry, lead.Score))
            .OrderBy(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .ToList();

        var selectionPool = ruleCandidates.Count > 0
            ? ruleCandidates
            : capacityPool
                .OrderBy(user => user.CreatedAtUtc)
                .ThenBy(user => user.Id)
                .ToList();

        var latest = await _assignmentRepository.GetLatestAsync(cancellationToken);
        var selected = SelectNextUser(selectionPool, latest?.UserId);

        var strategy = ruleCandidates.Count > 0 ? RuleBasedStrategy : RoundRobinStrategy;
        var ruleKey = ruleCandidates.Count > 0 ? BuildRuleKey(selected) : null;

        var assignment = new LeadAssignment(
            leadId,
            selected.Id,
            strategy,
            ruleKey);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);

        _logger.LogInformation(
            "Lead {LeadId} assigned to user {UserId} using {Strategy}.",
            leadId,
            selected.Id,
            strategy);

        return MapAssignment(assignment);
    }

    public async Task<LeadAssignmentResponse?> AssignLeadManuallyAsync(Guid leadId, ManualLeadAssignmentRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var assignment = new LeadAssignment(
            leadId,
            user.Id,
            ManualStrategy,
            request.Reason.Trim());

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        _assignmentProtectionStore.SetManualProtection(leadId, request.ProtectFromAutoOverwrite);

        return MapAssignment(assignment);
    }

    public async Task<LeadAssignmentResponse?> GetLatestByLeadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetLatestByLeadIdAsync(leadId, cancellationToken);
        return assignment is null ? null : MapAssignment(assignment);
    }

    public async Task<IReadOnlyList<LeadAssignmentResponse>> GetAssignmentsAsync(CancellationToken cancellationToken)
    {
        var assignments = await _assignmentRepository.GetAllAsync(cancellationToken);
        return assignments.Select(MapAssignment).ToList();
    }

    public async Task<AssignmentCapacityLoadResponse> GetCapacityLoadAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var allAssignments = await _assignmentRepository.GetAllAsync(cancellationToken);
        var currentLoadByUser = BuildCurrentLoadByUser(allAssignments);

        return new AssignmentCapacityLoadResponse
        {
            Users = users.Select(user =>
            {
                var currentLoad = currentLoadByUser.TryGetValue(user.Id, out var load) ? load : 0;
                return new AssignmentUserCapacityItem
                {
                    UserId = user.Id,
                    CurrentLoad = currentLoad,
                    MaxActiveLeads = user.MaxActiveLeads,
                    IsAtCapacity = currentLoad >= user.MaxActiveLeads
                };
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<AssignmentAuditEntryResponse>> GetAuditAsync(Guid? leadId, Guid? userId, int take, CancellationToken cancellationToken)
    {
        var boundedTake = Math.Clamp(take, 1, 500);
        var assignments = await _assignmentRepository.GetAllAsync(cancellationToken);

        var filtered = assignments
            .Where(x => leadId is null || x.LeadId == leadId.Value)
            .Where(x => userId is null || x.UserId == userId.Value)
            .OrderByDescending(x => x.AssignedAtUtc)
            .Take(boundedTake)
            .Select(x => new AssignmentAuditEntryResponse
            {
                AssignmentId = x.Id,
                LeadId = x.LeadId,
                UserId = x.UserId,
                Strategy = x.Strategy,
                RuleKey = x.RuleKey,
                IsManualProtected = _assignmentProtectionStore.IsManualProtected(x.LeadId),
                AssignedAtUtc = x.AssignedAtUtc
            })
            .ToList();

        return filtered;
    }

    public async Task<AssignmentFairnessResponse> GetFairnessAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetActiveAsync(cancellationToken);
        var assignments = await _assignmentRepository.GetAllAsync(cancellationToken);
        var currentLoadByUser = BuildCurrentLoadByUser(assignments);

        var loads = users
            .Select(user => currentLoadByUser.TryGetValue(user.Id, out var load) ? load : 0)
            .ToList();

        var total = loads.Sum();
        var avg = loads.Count == 0 ? 0m : decimal.Round((decimal)loads.Average(), 2);
        var variance = loads.Count == 0
            ? 0d
            : loads.Select(x => Math.Pow(x - (double)avg, 2)).Average();
        var stdDev = decimal.Round((decimal)Math.Sqrt(variance), 2);

        return new AssignmentFairnessResponse
        {
            TotalAssignments = total,
            AverageAssignmentsPerUser = avg,
            MaxAssignmentsBySingleUser = loads.Count == 0 ? 0 : loads.Max(),
            MinAssignmentsBySingleUser = loads.Count == 0 ? 0 : loads.Min(),
            StandardDeviation = stdDev,
            HasImbalanceRisk = loads.Count > 1 && stdDev > (avg * 0.5m),
            Distribution = users.Select(user =>
            {
                var assigned = currentLoadByUser.TryGetValue(user.Id, out var load) ? load : 0;
                return new AssignmentUserDistributionItem
                {
                    UserId = user.Id,
                    AssignedLeads = assigned,
                    SharePercent = total == 0 ? 0 : decimal.Round((assigned * 100m) / total, 2)
                };
            }).ToList()
        };
    }

    private async Task RebalanceUnavailableUserAssignmentsAsync(Guid unavailableUserId, CancellationToken cancellationToken)
    {
        var allAssignments = await _assignmentRepository.GetAllAsync(cancellationToken);
        var latestByLead = allAssignments
            .GroupBy(x => x.LeadId)
            .Select(group => group.OrderByDescending(x => x.AssignedAtUtc).ThenByDescending(x => x.Id).First())
            .Where(x => x.UserId == unavailableUserId)
            .Where(x => x.Strategy != ManualStrategy)
            .Where(x => !_assignmentProtectionStore.IsManualProtected(x.LeadId))
            .ToList();

        if (latestByLead.Count == 0)
        {
            return;
        }

        var activeUsers = await _userRepository.GetActiveAsync(cancellationToken);
        var eligible = activeUsers.Where(x => x.Id != unavailableUserId).OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToList();
        if (eligible.Count == 0)
        {
            return;
        }

        var currentLoadByUser = BuildCurrentLoadByUser(allAssignments);

        foreach (var item in latestByLead)
        {
            var target = eligible
                .OrderBy(user => currentLoadByUser.TryGetValue(user.Id, out var load) ? load : 0)
                .ThenBy(user => user.CreatedAtUtc)
                .ThenBy(user => user.Id)
                .First();

            var reassignment = new LeadAssignment(
                item.LeadId,
                target.Id,
                RebalanceStrategy,
                "availability_changed");

            await _assignmentRepository.AddAsync(reassignment, cancellationToken);
            currentLoadByUser[target.Id] = (currentLoadByUser.TryGetValue(target.Id, out var load) ? load : 0) + 1;
        }
    }

    private static bool MatchesAssignmentRules(AssignmentUser user, string leadCountry, string? leadIndustry, int leadScore)
    {
        if (!string.IsNullOrWhiteSpace(user.PreferredCountry)
            && !string.Equals(user.PreferredCountry, leadCountry, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(user.PreferredIndustry)
            && !string.Equals(user.PreferredIndustry, leadIndustry, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (user.MinScoreToAssign.HasValue && leadScore < user.MinScoreToAssign.Value)
        {
            return false;
        }

        return true;
    }

    private static bool HasExplicitAssignmentRules(AssignmentUser user)
    {
        return !string.IsNullOrWhiteSpace(user.PreferredCountry)
            || !string.IsNullOrWhiteSpace(user.PreferredIndustry)
            || user.MinScoreToAssign.HasValue;
    }

    private static string? BuildRuleKey(AssignmentUser user)
    {
        var keys = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.PreferredCountry))
        {
            keys.Add("country");
        }

        if (!string.IsNullOrWhiteSpace(user.PreferredIndustry))
        {
            keys.Add("industry");
        }

        if (user.MinScoreToAssign.HasValue)
        {
            keys.Add("score");
        }

        return keys.Count == 0 ? null : string.Join("+", keys);
    }

    private static Dictionary<Guid, int> BuildCurrentLoadByUser(IReadOnlyList<LeadAssignment> assignments)
    {
        return assignments
            .GroupBy(x => x.LeadId)
            .Select(group => group.OrderByDescending(x => x.AssignedAtUtc).ThenByDescending(x => x.Id).First())
            .GroupBy(x => x.UserId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static AssignmentUser SelectNextUser(IReadOnlyList<AssignmentUser> orderedUsers, Guid? lastUserId)
    {
        if (orderedUsers.Count == 1 || lastUserId is null)
        {
            return orderedUsers[0];
        }

        var currentIndex = orderedUsers
            .Select((user, index) => new { user.Id, index })
            .Where(x => x.Id == lastUserId.Value)
            .Select(x => (int?)x.index)
            .FirstOrDefault();

        if (currentIndex is null)
        {
            return orderedUsers[0];
        }

        var nextIndex = (currentIndex.Value + 1) % orderedUsers.Count;
        return orderedUsers[nextIndex];
    }

    private static string NormalizeName(string fullName)
    {
        return fullName.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static AssignmentUserResponse MapUser(AssignmentUser user)
    {
        return new AssignmentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            PreferredCountry = user.PreferredCountry,
            PreferredIndustry = user.PreferredIndustry,
            MaxActiveLeads = user.MaxActiveLeads,
            MinScoreToAssign = user.MinScoreToAssign,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    private static LeadAssignmentResponse MapAssignment(LeadAssignment assignment)
    {
        return new LeadAssignmentResponse
        {
            Id = assignment.Id,
            LeadId = assignment.LeadId,
            UserId = assignment.UserId,
            Strategy = assignment.Strategy,
            RuleKey = assignment.RuleKey,
            AssignedAtUtc = assignment.AssignedAtUtc
        };
    }
}
