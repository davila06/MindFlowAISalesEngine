using Api.Domain.Companies;
using Api.Domain.Observability;
using Api.Domain.Contacts;
using Api.Domain.Email;
using Api.Domain.FollowUp;
using Api.Domain.Leads;
using Api.Domain.Onboarding;
using Api.Domain.Proposals;
using Api.Domain.Pipeline;
using Api.Domain.Assignment;
using Api.Domain.Rules;
using Api.Domain.Security;
using Api.Application.Common.Interfaces;
using Api.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class LeadsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public LeadsDbContext(DbContextOptions<LeadsDbContext> options)
        : this(options, new TenantContext())
    {
    }

    public LeadsDbContext(DbContextOptions<LeadsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityStageHistory> OpportunityStageHistories => Set<OpportunityStageHistory>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();
    public DbSet<EmailStopListEntry> EmailStopListEntries => Set<EmailStopListEntry>();
    public DbSet<EmailDispatchJob> EmailDispatchJobs => Set<EmailDispatchJob>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<FollowUpJob> FollowUpJobs => Set<FollowUpJob>();
    public DbSet<FollowUpPolicySettings> FollowUpPolicySettings => Set<FollowUpPolicySettings>();
    public DbSet<AssignmentUser> AssignmentUsers => Set<AssignmentUser>();
    public DbSet<LeadAssignment> LeadAssignments => Set<LeadAssignment>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RuleCondition> RuleConditions => Set<RuleCondition>();
    public DbSet<RuleAction> RuleActions => Set<RuleAction>();
    public DbSet<RuleExecutionLog> RuleExecutionLogs => Set<RuleExecutionLog>();
    public DbSet<RuleRevision> RuleRevisions => Set<RuleRevision>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalTemplate> ProposalTemplates => Set<ProposalTemplate>();
    public DbSet<ProposalReminderJob> ProposalReminderJobs => Set<ProposalReminderJob>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<OnboardingWelcomeJob> OnboardingWelcomeJobs => Set<OnboardingWelcomeJob>();
    public DbSet<ObservabilityMetricRecord> ObservabilityMetricRecords => Set<ObservabilityMetricRecord>();
    public DbSet<ObservabilityAggregateBatch> ObservabilityAggregateBatches => Set<ObservabilityAggregateBatch>();
    public DbSet<ObservabilityEndpointAggregationState> ObservabilityEndpointAggregationStates => Set<ObservabilityEndpointAggregationState>();
    public DbSet<ObservabilityAggregationCheckpoint> ObservabilityAggregationCheckpoints => Set<ObservabilityAggregationCheckpoint>();
    public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<PoisonQueueRemediationRun> PoisonQueueRemediationRuns => Set<PoisonQueueRemediationRun>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<LeadAuditSnapshot> LeadAuditSnapshots => Set<LeadAuditSnapshot>();
    public DbSet<DataRetentionRun> DataRetentionRuns => Set<DataRetentionRun>();

    public override int SaveChanges()
    {
        ApplyTenantOnNewEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantOnNewEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantOnNewEntities()
    {
        var tenantId = string.IsNullOrWhiteSpace(_tenantContext.TenantId)
            ? TenantContext.DefaultTenantId
            : _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var tenantProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
            if (tenantProperty is null)
            {
                continue;
            }

            var current = tenantProperty.CurrentValue?.ToString();
            if (string.IsNullOrWhiteSpace(current))
            {
                tenantProperty.CurrentValue = tenantId;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.Source).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Channel).IsRequired().HasMaxLength(100).HasDefaultValue("inbound");
            entity.Property(x => x.Campaign).IsRequired().HasMaxLength(120).HasDefaultValue("organic");
            entity.Property(x => x.Country).IsRequired().HasMaxLength(2).HasDefaultValue("xx");
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Score).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.Priority).IsRequired().HasMaxLength(16).HasDefaultValue("Low");
            entity.Property(x => x.ScoringVersion).IsRequired().HasMaxLength(16).HasDefaultValue("unscored");
            entity.Property(x => x.ScoredAtUtc);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.Phone);
            entity.HasIndex(x => x.Source);
            entity.HasIndex(x => x.Channel);
            entity.HasIndex(x => x.Score);
            entity.HasIndex(x => x.Priority);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(160);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.DeletedAtUtc);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Phone).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId && !x.IsDeleted);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Industry).IsRequired().HasMaxLength(120).HasDefaultValue("unknown");
            entity.Property(x => x.Website).HasMaxLength(2048);
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.DeletedAtUtc);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Industry);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId && !x.IsDeleted);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PipelineStage>(entity =>
        {
            entity.ToTable("PipelineStages");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Order).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(16);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex("TenantId", nameof(PipelineStage.Name)).IsUnique();
            entity.HasIndex("TenantId", nameof(PipelineStage.Order)).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("Opportunities");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.StageId).IsRequired();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(180);
            entity.Property(x => x.Value).HasColumnType("TEXT");
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.StageId);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PipelineStage>()
                .WithMany()
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OpportunityStageHistory>(entity =>
        {
            entity.ToTable("OpportunityStageHistory");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.OpportunityId).IsRequired();
            entity.Property(x => x.FromStageId).IsRequired();
            entity.Property(x => x.ToStageId).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.Actor).IsRequired().HasMaxLength(120).HasDefaultValue("system");
            entity.Property(x => x.IsAutomated).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.ChangedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.OpportunityId);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Opportunity>()
                .WithMany()
                .HasForeignKey(x => x.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PipelineStage>()
                .WithMany()
                .HasForeignKey(x => x.FromStageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PipelineStage>()
                .WithMany()
                .HasForeignKey(x => x.ToStageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SmtpSettings>(entity =>
        {
            entity.ToTable("SmtpSettings");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.ProviderType).IsRequired().HasMaxLength(20).HasDefaultValue(Api.Domain.Email.SmtpSettings.SmtpProviderType);
            entity.Property(x => x.ProviderBaseUrl).HasMaxLength(500);
            entity.Property(x => x.ApiKey).HasMaxLength(500);
            entity.Property(x => x.Host).IsRequired().HasMaxLength(253);
            entity.Property(x => x.Port).IsRequired();
            entity.Property(x => x.Username).IsRequired().HasMaxLength(320);
            entity.Property(x => x.Password).IsRequired().HasMaxLength(500);
            entity.Property(x => x.FromEmail).IsRequired().HasMaxLength(320);
            entity.Property(x => x.FromName).HasMaxLength(100);
            entity.Property(x => x.EnableSsl).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmailDispatchJob>(entity =>
        {
            entity.ToTable("EmailDispatchJobs");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
            entity.Property(x => x.ProviderType).IsRequired().HasMaxLength(20);
            entity.Property(x => x.ToEmail).HasMaxLength(320);
            entity.Property(x => x.Subject).IsRequired().HasMaxLength(500);
            entity.Property(x => x.BodyHtml).IsRequired();
            entity.Property(x => x.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.AttachmentFileName).HasMaxLength(260);
            entity.Property(x => x.AttachmentContentType).HasMaxLength(100);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.AttemptCount).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.DueAtUtc).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DueAtUtc);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmailStopListEntry>(entity =>
        {
            entity.ToTable("EmailStopListEntries");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(320);
            entity.Property(x => x.Reason).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EmailTemplates");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Version).IsRequired().HasDefaultValue(1);
            entity.Property(x => x.Subject).IsRequired().HasMaxLength(500);
            entity.Property(x => x.BodyHtml).IsRequired();
            entity.Property(x => x.RequiredVariablesSerialized).IsRequired().HasDefaultValue(string.Empty);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.IsCurrent).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex("TenantId", nameof(EmailTemplate.Name), nameof(EmailTemplate.Version)).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EmailLogs");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
            entity.Property(x => x.ToEmail).HasMaxLength(320);
            entity.Property(x => x.Subject).HasMaxLength(500);
            entity.Property(x => x.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.Succeeded).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.SentAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.LeadId);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<FollowUpJob>(entity =>
        {
            entity.ToTable("FollowUpJobs");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.ToEmail).HasMaxLength(320);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.AttemptNumber).IsRequired();
            entity.Property(x => x.ScheduledAtUtc).IsRequired();
            entity.Property(x => x.DueAtUtc).IsRequired();
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.Status);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<FollowUpPolicySettings>(entity =>
        {
            entity.ToTable("FollowUpPolicySettings");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.QuietHoursEnabled).IsRequired();
            entity.Property(x => x.QuietHoursStartHourUtc).IsRequired();
            entity.Property(x => x.QuietHoursEndHourUtc).IsRequired();
            entity.Property(x => x.RulesJson).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<AssignmentUser>(entity =>
        {
            entity.ToTable("AssignmentUsers");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(320);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.PreferredCountry).HasMaxLength(16);
            entity.Property(x => x.PreferredIndustry).HasMaxLength(120);
            entity.Property(x => x.MaxActiveLeads).IsRequired().HasDefaultValue(100);
            entity.Property(x => x.MinScoreToAssign);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<LeadAssignment>(entity =>
        {
            entity.ToTable("LeadAssignments");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Strategy).IsRequired().HasMaxLength(50);
            entity.Property(x => x.RuleKey).HasMaxLength(100);
            entity.Property(x => x.AssignedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.AssignedAtUtc);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AssignmentUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rule>(entity =>
        {
            entity.ToTable("Rules");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Trigger).IsRequired().HasMaxLength(100);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.Priority).IsRequired().HasDefaultValue(100);
            entity.Property(x => x.ConflictPolicy).IsRequired().HasMaxLength(20).HasDefaultValue("first_wins");
            entity.Property(x => x.ExecutionStartHourUtc);
            entity.Property(x => x.ExecutionEndHourUtc);
            entity.Property(x => x.CooldownMinutes).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.AllowDestructiveActions).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.Version).IsRequired().HasDefaultValue(1);
            entity.Property(x => x.Environment).IsRequired().HasMaxLength(12).HasDefaultValue("dev");
            entity.Property(x => x.ApprovalStatus).IsRequired().HasMaxLength(20).HasDefaultValue("approved");
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.ApprovedAtUtc);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.Trigger);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.Priority);
            entity.HasIndex(x => x.Environment);
            entity.HasIndex(x => x.ApprovalStatus);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasMany(x => x.Conditions)
                .WithOne()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Actions)
                .WithOne()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleCondition>(entity =>
        {
            entity.ToTable("RuleConditions");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.RuleId).IsRequired();
            entity.Property(x => x.Field).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Operator).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Value).IsRequired().HasMaxLength(200);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.RuleId);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<RuleAction>(entity =>
        {
            entity.ToTable("RuleActions");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.RuleId).IsRequired();
            entity.Property(x => x.Type).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Value).IsRequired().HasMaxLength(200);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.RuleId);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<RuleExecutionLog>(entity =>
        {
            entity.ToTable("RuleExecutionLogs");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.RuleId).IsRequired();
            entity.Property(x => x.Trigger).IsRequired().HasMaxLength(100);
            entity.Property(x => x.EntityType).IsRequired().HasMaxLength(40);
            entity.Property(x => x.EntityId).IsRequired();
            entity.Property(x => x.Matched).IsRequired();
            entity.Property(x => x.Applied).IsRequired();
            entity.Property(x => x.ActionsAppliedCount).IsRequired();
            entity.Property(x => x.SkippedReason).HasMaxLength(120);
            entity.Property(x => x.DurationMs).HasColumnType("TEXT");
            entity.Property(x => x.ExecutedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.RuleId);
            entity.HasIndex(x => x.EntityId);
            entity.HasIndex(x => x.ExecutedAtUtc);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Rule>()
                .WithMany()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleRevision>(entity =>
        {
            entity.ToTable("RuleRevisions");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.RuleId).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.SnapshotJson).IsRequired();
            entity.Property(x => x.Reason).IsRequired().HasMaxLength(120);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.RuleId);
            entity.HasIndex(x => x.Version);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Rule>()
                .WithMany()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Proposal>(entity =>
        {
            entity.ToTable("Proposals");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(180);
            entity.Property(x => x.Amount).HasColumnType("TEXT");
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(8);
            entity.Property(x => x.RecipientName).HasMaxLength(160);
            entity.Property(x => x.RecipientEmail).HasMaxLength(320);
            entity.Property(x => x.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.TemplateVersion).IsRequired().HasDefaultValue(1);
            entity.Property(x => x.PdfFileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.PdfContent).IsRequired();
            entity.Property(x => x.TrackingToken).IsRequired().HasMaxLength(64);
            entity.Property(x => x.ViewCount).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.ExpiresAtUtc);
            entity.Property(x => x.SignedAtUtc);
            entity.Property(x => x.SignedByName).HasMaxLength(160);
            entity.Property(x => x.SignedByEmail).HasMaxLength(320);
            entity.Property(x => x.RenewedFromProposalId);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.TrackingToken).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

            modelBuilder.Entity<ProposalTemplate>(entity =>
            {
                entity.ToTable("ProposalTemplates");
                entity.HasKey(x => x.Id);
                entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.Property(x => x.DisplayName).IsRequired().HasMaxLength(160);
                entity.Property(x => x.HtmlBody).IsRequired();
                entity.Property(x => x.Version).IsRequired();
                entity.Property(x => x.IsCurrent).IsRequired();
                entity.Property(x => x.CreatedAtUtc).IsRequired();
                entity.HasIndex("TenantId");
                entity.HasIndex(x => new { x.Name, x.Version }).IsUnique();
                entity.HasIndex(x => new { x.Name, x.IsCurrent });
                entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            });

        modelBuilder.Entity<ProposalReminderJob>(entity =>
        {
            entity.ToTable("ProposalReminderJobs");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.ProposalId).IsRequired();
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.ToEmail).HasMaxLength(320);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.AttemptNumber).IsRequired();
            entity.Property(x => x.ScheduledAtUtc).IsRequired();
            entity.Property(x => x.DueAtUtc).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.ProposalId).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Proposal>()
                .WithMany()
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.Email).IsRequired().HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.Segment).IsRequired().HasMaxLength(40).HasDefaultValue("standard");
            entity.Property(x => x.PlaybookKey).IsRequired().HasMaxLength(80).HasDefaultValue("standard-onboarding");
            entity.Property(x => x.HealthScore).HasColumnType("TEXT");
            entity.Property(x => x.TrackingToken).IsRequired().HasMaxLength(64);
            entity.Property(x => x.TrackingActivations).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.LeadId).IsUnique();
            entity.HasIndex(x => x.TrackingToken).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OnboardingTask>(entity =>
        {
            entity.ToTable("OnboardingTasks");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.CustomerId).IsRequired();
            entity.Property(x => x.Key).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.DependencyKeysSerialized).HasMaxLength(400);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.DueAtUtc);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => new { x.CustomerId, x.Key }).IsUnique();
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

            modelBuilder.Entity<OnboardingWelcomeJob>(entity =>
            {
                entity.ToTable("OnboardingWelcomeJobs");
                entity.HasKey(x => x.Id);
                entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
                entity.Property(x => x.CustomerId).IsRequired();
                entity.Property(x => x.LeadId).IsRequired();
                entity.Property(x => x.ToEmail).HasMaxLength(320);
                entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
                entity.Property(x => x.AttemptNumber).IsRequired();
                entity.Property(x => x.ScheduledAtUtc).IsRequired();
                entity.Property(x => x.DueAtUtc).IsRequired();
                entity.Property(x => x.ExecutedAtUtc);
                entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
                entity.HasIndex("TenantId");
                entity.HasIndex(x => x.CustomerId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.DueAtUtc);
                entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
                entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

        modelBuilder.Entity<AlertThreshold>(entity =>
        {
            entity.ToTable("AlertThresholds");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.MaxErrorRatePercent).HasColumnType("TEXT");
            entity.Property(x => x.MaxAverageLatencyMs).HasColumnType("TEXT");
            entity.Property(x => x.NotificationEmail).IsRequired().HasMaxLength(320);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.WebhookUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.EndpointName);
            entity.HasIndex(x => x.IsActive);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
        });

        modelBuilder.Entity<AlertEvent>(entity =>
        {
            entity.ToTable("AlertEvents");
            entity.HasKey(x => x.Id);
            entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
            entity.Property(x => x.ThresholdId).IsRequired();
            entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.MetricName).IsRequired().HasMaxLength(64);
            entity.Property(x => x.ObservedValue).HasColumnType("TEXT");
            entity.Property(x => x.ThresholdValue).HasColumnType("TEXT");
            entity.Property(x => x.TriggeredAtUtc).IsRequired();
            entity.Property(x => x.NotificationSent).IsRequired();
                entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("open");
                entity.Property(x => x.AcknowledgedBy).HasMaxLength(120);
                entity.Property(x => x.AcknowledgedAtUtc);
                entity.Property(x => x.SnoozedUntilUtc);
                entity.Property(x => x.ResolvedBy).HasMaxLength(120);
                entity.Property(x => x.ResolvedAtUtc);
                entity.Property(x => x.StatusNotes).HasMaxLength(500);
            entity.HasIndex("TenantId");
            entity.HasIndex(x => x.EndpointName);
            entity.HasIndex(x => x.MetricName);
            entity.HasIndex(x => x.TriggeredAtUtc);
            entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            entity.HasOne<AlertThreshold>()
                .WithMany()
                .HasForeignKey(x => x.ThresholdId)
                .OnDelete(DeleteBehavior.Restrict);
        });

            modelBuilder.Entity<PoisonQueueRemediationRun>(entity =>
            {
                entity.ToTable("PoisonQueueRemediationRuns");
                entity.HasKey(x => x.Id);
                entity.Property<string>("TenantId").IsRequired().HasMaxLength(64).HasDefaultValue(TenantContext.DefaultTenantId);
                entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
                entity.Property(x => x.JobType).IsRequired().HasMaxLength(64);
                entity.Property(x => x.Severity).IsRequired().HasMaxLength(16);
                entity.Property(x => x.RecommendedAction).IsRequired().HasMaxLength(300);
                entity.Property(x => x.RemediationPath).HasMaxLength(200);
                entity.Property(x => x.Outcome).IsRequired().HasMaxLength(16);
                entity.Property(x => x.ExecutedBy).IsRequired().HasMaxLength(120);
                entity.Property(x => x.ExecutedAtUtc).IsRequired();
                entity.Property(x => x.DetectedAtUtc);
                entity.Property(x => x.ResolutionLatencyMinutes).HasColumnType("TEXT");
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasIndex("TenantId");
                entity.HasIndex(x => x.JobType);
                entity.HasIndex(x => x.Outcome);
                entity.HasIndex(x => x.ExecutedAtUtc);
                entity.HasQueryFilter(x => EF.Property<string>(x, "TenantId") == _tenantContext.TenantId);
            });

        modelBuilder.Entity<ObservabilityMetricRecord>(entity =>
        {
            entity.ToTable("ObservabilityMetricRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.RequestCount).IsRequired();
            entity.Property(x => x.SuccessCount).IsRequired();
            entity.Property(x => x.ErrorCount).IsRequired();
            entity.Property(x => x.AverageLatencyMs).HasColumnType("TEXT");
            entity.Property(x => x.RecordedAtUtc).IsRequired();
            entity.HasIndex(x => x.RecordedAtUtc);
            entity.HasIndex(x => x.EndpointName);
        });

        modelBuilder.Entity<ObservabilityAggregateBatch>(entity =>
        {
            entity.ToTable("ObservabilityAggregateBatches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.WindowStartUtc).IsRequired();
            entity.Property(x => x.WindowEndUtc).IsRequired();
            entity.Property(x => x.IncrementalRequestCount).IsRequired();
            entity.Property(x => x.IncrementalSuccessCount).IsRequired();
            entity.Property(x => x.IncrementalErrorCount).IsRequired();
            entity.Property(x => x.TotalLatencyMs).HasColumnType("TEXT");
            entity.Property(x => x.SampleCount).IsRequired();
            entity.Property(x => x.LastSourceRecordedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.WindowStartUtc);
            entity.HasIndex(x => x.EndpointName);
            entity.HasIndex(x => new { x.EndpointName, x.WindowStartUtc, x.WindowEndUtc }).IsUnique();
        });

        modelBuilder.Entity<ObservabilityEndpointAggregationState>(entity =>
        {
            entity.ToTable("ObservabilityEndpointAggregationStates");
            entity.HasKey(x => x.EndpointName);
            entity.Property(x => x.EndpointName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastRequestCount).IsRequired();
            entity.Property(x => x.LastSuccessCount).IsRequired();
            entity.Property(x => x.LastErrorCount).IsRequired();
            entity.Property(x => x.LastRecordedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ObservabilityAggregationCheckpoint>(entity =>
        {
            entity.ToTable("ObservabilityAggregationCheckpoints");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastProcessedRecordedAtUtc);
            entity.Property(x => x.LastProcessedRecordId).HasMaxLength(64);
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("AdminAuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Target).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Details).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.UserRole).IsRequired().HasMaxLength(32);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.TenantId);
            entity.HasIndex(x => x.Action);
        });

        modelBuilder.Entity<LeadAuditSnapshot>(entity =>
        {
            entity.ToTable("LeadAuditSnapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LeadId).IsRequired();
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Actor).IsRequired().HasMaxLength(160);
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasOne<Lead>()
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DataRetentionRun>(entity =>
        {
            entity.ToTable("DataRetentionRuns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmailLogsRemoved).IsRequired();
            entity.Property(x => x.AlertEventsRemoved).IsRequired();
            entity.Property(x => x.AdminAuditLogsRemoved).IsRequired();
            entity.Property(x => x.ExecutedAtUtc).IsRequired();
            entity.HasIndex(x => x.ExecutedAtUtc);
        });
    }
}

