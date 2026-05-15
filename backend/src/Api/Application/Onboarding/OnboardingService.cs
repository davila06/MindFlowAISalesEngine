using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;
using Api.Application.Email;
using Api.Application.Observability;
using Api.Contracts;
using Api.Domain.Leads;
using Api.Domain.Onboarding;
using Microsoft.Extensions.Logging;

namespace Api.Application.Onboarding;

public class OnboardingService : IOnboardingService
{
    private sealed record PlaybookTaskDefinition(string Key, string Title, string[] Dependencies, int DueDays);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<PlaybookTaskDefinition>> Playbooks =
        new Dictionary<string, IReadOnlyList<PlaybookTaskDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["standard-onboarding"] =
            [
                new("kickoff-call", "Schedule kickoff call", [], 1),
                new("requirements-checklist", "Collect requirements checklist", ["kickoff-call"], 3),
                new("workspace-setup", "Set up workspace and integrations", ["requirements-checklist"], 5)
            ],
            ["enterprise-onboarding"] =
            [
                new("kickoff-call", "Schedule executive kickoff", [], 1),
                new("security-review", "Complete security review", ["kickoff-call"], 3),
                new("requirements-checklist", "Collect enterprise requirements", ["security-review"], 5),
                new("workspace-setup", "Provision workspace and integrations", ["requirements-checklist"], 7)
            ]
        };

    private static readonly TimeSpan WelcomeInitialDelay = TimeSpan.Zero;
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMinutes(20);
    private const int MaxRetryAttempts = 3;

    private readonly ILeadRepository _leadRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOnboardingTaskRepository _onboardingTaskRepository;
    private readonly IOnboardingWelcomeJobRepository _welcomeJobRepository;
    private readonly IEmailService _emailService;
    private readonly IPoisonQueueAlertService _poisonQueueAlertService;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        ILeadRepository leadRepository,
        ICustomerRepository customerRepository,
        IOnboardingTaskRepository onboardingTaskRepository,
        IOnboardingWelcomeJobRepository welcomeJobRepository,
        IEmailService emailService,
        IPoisonQueueAlertService poisonQueueAlertService,
        ILogger<OnboardingService> logger)
    {
        _leadRepository = leadRepository;
        _customerRepository = customerRepository;
        _onboardingTaskRepository = onboardingTaskRepository;
        _welcomeJobRepository = welcomeJobRepository;
        _emailService = emailService;
        _poisonQueueAlertService = poisonQueueAlertService;
        _logger = logger;
    }

    public async Task EnsureForWonOpportunityAsync(Guid leadId, CancellationToken cancellationToken)
    {
        if (leadId == Guid.Empty)
        {
            return;
        }

        var existing = await _customerRepository.GetByLeadIdAsync(leadId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null || string.IsNullOrWhiteSpace(lead.Email))
        {
            return;
        }

        var segment = ResolveSegment(lead);
        var playbookKey = ResolvePlaybookKey(lead);
        var customer = new Customer(leadId, lead.Email, lead.Phone, segment, playbookKey);
        await _customerRepository.AddAsync(customer, cancellationToken);

        var tasks = BuildPlaybookTasks(customer.Id, playbookKey);
        await _onboardingTaskRepository.AddRangeAsync(tasks, cancellationToken);
        await RecalculateCustomerHealthAsync(customer, tasks, persistCustomerChanges: true, cancellationToken);

        var welcomeJob = new OnboardingWelcomeJob(
            customer.Id,
            customer.LeadId,
            customer.Email,
            DateTime.UtcNow.Add(WelcomeInitialDelay),
            1);

        await _welcomeJobRepository.AddAsync(welcomeJob, cancellationToken);
        await ExecuteDueWelcomeJobsAsync(cancellationToken);
    }

    public async Task ExecuteDueWelcomeJobsAsync(CancellationToken cancellationToken)
    {
        var dueJobs = await _welcomeJobRepository.GetScheduledDueAsync(DateTime.UtcNow, cancellationToken);
        if (dueJobs.Count == 0)
        {
            return;
        }

        foreach (var job in dueJobs)
        {
            var customer = await _customerRepository.GetByIdAsync(job.CustomerId, cancellationToken);
            if (customer is null)
            {
                await HandleWelcomeFailureAsync(job, "CustomerNotFound", cancellationToken);
                await _welcomeJobRepository.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var trackingUrl = $"/api/onboarding/track/{customer.TrackingToken}";
                var sent = await _emailService.SendCustomerWelcomeAsync(
                    customer.LeadId,
                    customer.Email,
                    trackingUrl,
                    cancellationToken);

                if (sent)
                {
                    job.MarkSent();
                }
                else
                {
                    await HandleWelcomeFailureAsync(job, "WelcomeNotSent", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleWelcomeFailureAsync(job, ex.Message, cancellationToken);
            }

            await _welcomeJobRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForceWelcomeJobDueAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var job = await _welcomeJobRepository.GetLatestByCustomerIdAsync(customerId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.ForceDue();
        await _welcomeJobRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingWelcomeJobResponse>> GetWelcomeDeadLetterAsync(CancellationToken cancellationToken)
    {
        var jobs = await _welcomeJobRepository.GetDeadLetterAsync(cancellationToken);
        return jobs.Select(MapWelcomeJob).ToList();
    }

    public async Task<IReadOnlyList<OnboardingWelcomeJobResponse>> GetWelcomePoisonQueueAsync(CancellationToken cancellationToken)
    {
        var jobs = await _welcomeJobRepository.GetPoisonQueueAsync(cancellationToken);
        return jobs.Select(MapWelcomeJob).ToList();
    }

    public async Task RequeueWelcomeJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _welcomeJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        if (!string.Equals(job.Status, OnboardingWelcomeJobStatus.Failed, StringComparison.Ordinal)
            && !string.Equals(job.Status, OnboardingWelcomeJobStatus.Poisoned, StringComparison.Ordinal))
        {
            return;
        }

        job.Requeue(DateTime.UtcNow);
        await _welcomeJobRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListCustomersAsync(CancellationToken cancellationToken)
    {
        await EvaluateLifecycleAsync(cancellationToken);
        var customers = await _customerRepository.ListAsync(cancellationToken);
        return customers.Select(MapCustomer).ToList();
    }

    public async Task<CustomerResponse?> GetCustomerByLeadIdAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByLeadIdAsync(leadId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var tasks = await _onboardingTaskRepository.ListByCustomerIdAsync(customer.Id, cancellationToken);
        await RecalculateCustomerHealthAsync(customer, tasks, persistCustomerChanges: true, cancellationToken);
        return MapCustomer(customer);
    }

    public async Task<IReadOnlyList<OnboardingTaskResponse>> GetTasksByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var tasks = await _onboardingTaskRepository.ListByCustomerIdAsync(customerId, cancellationToken);
        return tasks.Select(MapTask).ToList();
    }

    public async Task<OnboardingTaskResponse?> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _onboardingTaskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var tasks = await _onboardingTaskRepository.ListByCustomerIdAsync(task.CustomerId, cancellationToken);
        var completedKeys = tasks.Where(x => string.Equals(x.Status, OnboardingTaskStatus.Completed, StringComparison.Ordinal))
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (task.HasPendingDependencies(completedKeys))
        {
            throw new OnboardingValidationException(new Dictionary<string, string[]>
            {
                ["dependencies"] = ["Task dependencies must be completed before this task can be closed."]
            });
        }

        task.MarkCompleted();
        await _onboardingTaskRepository.SaveChangesAsync(cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(task.CustomerId, cancellationToken);
        if (customer is not null)
        {
            await RecalculateCustomerHealthAsync(customer, tasks, persistCustomerChanges: true, cancellationToken);
        }

        return MapTask(task);
    }

    public async Task<OnboardingOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        await EvaluateLifecycleAsync(cancellationToken);
        var customers = await _customerRepository.ListAsync(cancellationToken);
        var tasks = await _onboardingTaskRepository.ListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var totalCustomers = customers.Count;
        var activatedCustomers = customers.Count(x => x.TrackingActivations > 0);
        var overdueTasks = tasks.Count(x => x.IsOverdue(now));

        return new OnboardingOverviewResponse
        {
            TotalCustomers = totalCustomers,
            OverdueTasks = overdueTasks,
            EarlyActivationRatePercent = totalCustomers == 0 ? 0 : Math.Round((decimal)activatedCustomers / totalCustomers * 100m, 2),
            AverageHealthScore = totalCustomers == 0 ? 0 : Math.Round(customers.Average(x => x.HealthScore), 2),
            AtRiskCustomers = customers.Count(x => string.Equals(x.Status, CustomerStatus.AtRisk, StringComparison.Ordinal)),
            ChurnRiskCustomers = customers.Count(x => string.Equals(x.Status, CustomerStatus.ChurnRisk, StringComparison.Ordinal))
        };
    }

    public async Task EvaluateLifecycleAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.ListAsync(cancellationToken);
        foreach (var customer in customers)
        {
            var tasks = await _onboardingTaskRepository.ListByCustomerIdAsync(customer.Id, cancellationToken);
            await RecalculateCustomerHealthAsync(customer, tasks, persistCustomerChanges: false, cancellationToken);
        }

        await _customerRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackAsync(string trackingToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trackingToken))
        {
            return;
        }

        var customer = await _customerRepository.GetByTrackingTokenAsync(trackingToken.Trim(), cancellationToken);
        if (customer is null)
        {
            return;
        }

        customer.TrackActivation();
        var tasks = await _onboardingTaskRepository.ListByCustomerIdAsync(customer.Id, cancellationToken);
        await RecalculateCustomerHealthAsync(customer, tasks, persistCustomerChanges: true, cancellationToken);
    }

    private static string ResolveSegment(Lead lead)
    {
        return lead.Score >= 80 || lead.Source.Contains("enterprise", StringComparison.OrdinalIgnoreCase)
            ? "enterprise"
            : "standard";
    }

    private static string ResolvePlaybookKey(Lead lead)
    {
        return ResolveSegment(lead) == "enterprise" ? "enterprise-onboarding" : "standard-onboarding";
    }

    private static List<OnboardingTask> BuildPlaybookTasks(Guid customerId, string playbookKey)
    {
        var definitions = Playbooks.TryGetValue(playbookKey, out var configured) ? configured : Playbooks["standard-onboarding"];
        return definitions
            .Select(definition => new OnboardingTask(
                customerId,
                definition.Key,
                definition.Title,
                definition.Dependencies,
                DateTime.UtcNow.AddDays(definition.DueDays)))
            .ToList();
    }

    private async Task RecalculateCustomerHealthAsync(
        Customer customer,
        IReadOnlyList<OnboardingTask> tasks,
        bool persistCustomerChanges,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(x => string.Equals(x.Status, OnboardingTaskStatus.Completed, StringComparison.Ordinal));
        var overdueTasks = tasks.Count(x => x.IsOverdue(now));
        var completionScore = totalTasks == 0 ? 0m : (decimal)completedTasks / totalTasks * 15m;
        var activationScore = customer.TrackingActivations > 0 ? 10m : 0m;
        var overduePenalty = overdueTasks * 25m;
        var health = Math.Clamp(80m + completionScore + activationScore - overduePenalty, 0m, 100m);

        customer.UpdateHealth(health);

        if (health < 40m)
        {
            customer.MarkChurnRisk();
        }
        else if (health < 75m)
        {
            customer.MarkAtRisk();
        }
        else
        {
            customer.MarkActive();
        }

        if (persistCustomerChanges)
        {
            await _customerRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private static CustomerResponse MapCustomer(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            LeadId = customer.LeadId,
            Email = customer.Email,
            Phone = customer.Phone,
            Status = customer.Status,
            Segment = customer.Segment,
            PlaybookKey = customer.PlaybookKey,
            HealthScore = customer.HealthScore,
            TrackingToken = customer.TrackingToken,
            TrackingActivations = customer.TrackingActivations,
            LastTrackingActivatedAtUtc = customer.LastTrackingActivatedAtUtc,
            CreatedAtUtc = customer.CreatedAtUtc
        };
    }

    private static OnboardingTaskResponse MapTask(OnboardingTask task)
    {
        return new OnboardingTaskResponse
        {
            Id = task.Id,
            CustomerId = task.CustomerId,
            Key = task.Key,
            Title = task.Title,
            Status = task.Status,
            DependencyKeys = task.DependencyKeys.ToList(),
            CreatedAtUtc = task.CreatedAtUtc,
            DueAtUtc = task.DueAtUtc,
            CompletedAtUtc = task.CompletedAtUtc
        };
    }

    private static OnboardingWelcomeJobResponse MapWelcomeJob(OnboardingWelcomeJob job)
    {
        return new OnboardingWelcomeJobResponse
        {
            Id = job.Id,
            CustomerId = job.CustomerId,
            LeadId = job.LeadId,
            ToEmail = PiiMasking.MaskEmail(job.ToEmail),
            Status = job.Status,
            AttemptNumber = job.AttemptNumber,
            ScheduledAtUtc = job.ScheduledAtUtc,
            DueAtUtc = job.DueAtUtc,
            ExecutedAtUtc = job.ExecutedAtUtc,
            ErrorMessage = job.ErrorMessage
        };
    }

    private async Task HandleWelcomeFailureAsync(OnboardingWelcomeJob job, string errorMessage, CancellationToken cancellationToken)
    {
        if (job.AttemptNumber >= MaxRetryAttempts)
        {
            job.MarkPoisoned(errorMessage);
            var queueDepth = await _welcomeJobRepository.CountPoisonedAsync(cancellationToken) + 1;
            await _poisonQueueAlertService.NotifyGrowthAsync("onboarding-welcome", queueDepth, cancellationToken);
            return;
        }

        var nextAttempt = job.AttemptNumber + 1;
        job.ScheduleRetry(DateTime.UtcNow.Add(GetRetryDelay(nextAttempt)), errorMessage);
        _logger.LogWarning(
            "Onboarding welcome job {JobId} scheduled retry attempt {AttemptNumber}",
            job.Id,
            nextAttempt);
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        return TimeSpan.FromMinutes(RetryBaseDelay.TotalMinutes * attemptNumber);
    }
}
