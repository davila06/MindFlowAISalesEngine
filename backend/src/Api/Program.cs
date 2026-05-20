
using Api.Application.Common.FeatureFlags;
using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;
using Api.Infrastructure.FeatureFlags;
using Api.Application.Companies;
using Api.Application.Contacts;
using Api.Application.AnalyticsAdvanced;
using Api.Application.Observability;
using Api.Infrastructure.Observability;
using Api.Application.CustomFields;
using Api.Application.Email;
using Api.Application.Assignment;
using Api.Application.Dashboard;
using Api.Application.DataGovernance;
using Api.Application.Leads;
using Api.Application.Onboarding;
using Api.Application.Pipeline;
using Api.Application.Proposals;
using Api.Application.RulesEngine;
using Api.Application.Scoring;
using Api.Application.Sequences;
using Api.Application.WhatsApp;
using Api.Domain.Email;
using Api.Domain.Pipeline;
using Api.Infrastructure.Assignment;
using Api.Infrastructure.Analytics;
using Api.Infrastructure.AnalyticsAdvanced;
using Api.Infrastructure.CustomFields;
using Api.Infrastructure.DataGovernance;
using Api.Infrastructure.Email;
using Api.Infrastructure.Events;
using Api.Infrastructure.Onboarding;
using Api.Infrastructure.Pipeline;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Leads;
using Api.Infrastructure.Proposals;
using Api.Infrastructure.RulesEngine;
using Api.Infrastructure.Scoring;
using Api.Infrastructure.Security;
using Api.Infrastructure.Sequences;
using Api.Infrastructure.Serialization;
using Api.Infrastructure.Tenancy;
using Api.Infrastructure.WhatsApp;
using Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Api.Application.FollowUp;
using Api.Contracts;
using Api.Application.Security;
using Api.Infrastructure.FollowUp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
// Operations KPIs
builder.Services.AddSingleton<Api.Application.Dashboard.IOperationsKpiService, Api.Application.Dashboard.InMemoryOperationsKpiService>();
// Omnichannel module registration
builder.Services.AddSingleton<Api.Application.Channels.IChannelMessageRepository, Api.Application.Channels.InMemoryChannelMessageRepository>();
builder.Services.AddScoped<Api.Application.Channels.IChannelDispatcher, Api.Application.Channels.EmailChannelDispatcher>();
// Workflows module registration
builder.Services.AddSingleton<Api.Application.Workflows.IWorkflowDefinitionRepository, Api.Application.Workflows.InMemoryWorkflowDefinitionRepository>();
builder.Services.AddScoped<Api.Application.Workflows.WorkflowDefinitionService>();
builder.Services.Configure<SecurityRuntimeOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<DataGovernanceOptions>(builder.Configuration.GetSection("DataGovernance"));
var securityOptions = builder.Configuration.GetSection("Security").Get<SecurityRuntimeOptions>() ?? new SecurityRuntimeOptions();
// AnalyticsAdvancedCache section controls in-memory cache TTL for heavy analytics snapshots.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
});
builder.Services.Configure<ApiBehaviorOptions>(options =>{
    options.InvalidModelStateResponseFactory = context =>    {
        var errors = context.ModelState            .Where(x => x.Value?.Errors.Count > 0)            .ToDictionary(                x => x.Key,                x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new ApiErrorResponse        {
            Code = DomainErrorCodes.ValidationError,            Message = "One or more validation errors occurred.",            TraceId = context.HttpContext.TraceIdentifier,            ValidationErrors = errors        }
);
    }
;
}
);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<LeadsDbContext>();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();
builder.Services.Configure<AnalyticsAdvancedCacheOptions>(builder.Configuration.GetSection("AnalyticsAdvancedCache"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)    .AddJwtBearer(options =>    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters        {
            ValidateIssuer = true,            ValidateAudience = true,            ValidateLifetime = true,            ValidateIssuerSigningKey = true,            ValidIssuer = securityOptions.JwtIssuer,            ValidAudience = securityOptions.JwtAudience,            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityOptions.JwtSigningKey)),            ClockSkew = TimeSpan.FromMinutes(1)        }
;
    }
);
builder.Services.AddAuthorization(options =>{
    options.AddPolicy(SecurityPolicies.AdminOnly, policy =>        policy.RequireAssertion(context => context.User.IsInRole(UserRoles.Admin)));
    options.AddPolicy(SecurityPolicies.Operator, policy =>        policy.RequireAssertion(context =>            context.User.IsInRole(UserRoles.Admin)            || context.User.IsInRole(UserRoles.Sales)));
}
);
builder.Services.AddCors(options =>{
    options.AddPolicy("NovamindCors", policy =>    {
        policy.WithOrigins(securityOptions.AllowedCorsOrigins)            .AllowAnyHeader()            .AllowAnyMethod();
    }
);
}
);
builder.Services.AddRateLimiter(options =>{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api-by-tenant-ip", context =>    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].ToString();
        var key = string.IsNullOrWhiteSpace(tenantId)            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"            : tenantId.Trim().ToLowerInvariant();
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions        {
            PermitLimit = 120,            Window = TimeSpan.FromMinutes(1),            QueueLimit = 20,            QueueProcessingOrder = QueueProcessingOrder.OldestFirst        }
);
    }
);
}
);
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<ILeadIntakeFailureStore, InMemoryLeadIntakeFailureStore>();
builder.Services.AddSingleton<ITenantDataGovernanceStore, InMemoryTenantDataGovernanceStore>();
builder.Services.AddSingleton<IStageWipLimitStore, InMemoryStageWipLimitStore>();
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "sqlite";
var defaultSqliteConnection = "Data Source=leads.db";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? defaultSqliteConnection;
builder.Services.AddDbContext<LeadsDbContext>(options =>
{
    switch (databaseProvider)
    {
        case "sqlserver":
            options.UseSqlServer(connectionString);
            break;
        case "postgres":
        case "postgresql":
            options.UseNpgsql(connectionString);
            break;
        default:
            options.UseSqlite(connectionString);
            break;
    }
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<ILeadAuditSnapshotRepository, LeadAuditSnapshotRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IPipelineStageRepository, PipelineStageRepository>();
builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();
builder.Services.AddScoped<IOpportunityStageHistoryRepository, OpportunityStageHistoryRepository>();
builder.Services.AddScoped<ILeadIntakeService, LeadIntakeService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IPipelineService, PipelineService>();
builder.Services.AddScoped<ILeadCreatedEventPublisher, LeadCreatedEventPublisher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ISmtpSettingsRepository, SmtpSettingsRepository>();
builder.Services.AddScoped<IEmailDispatchJobRepository, EmailDispatchJobRepository>();
builder.Services.AddScoped<IEmailStopListRepository, EmailStopListRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailDispatchService, EmailDispatchService>();
// Activity Timeline
builder.Services.AddScoped<ILeadActivityRepository, LeadActivityRepository>();
builder.Services.AddScoped<ILeadActivityService, LeadActivityService>();
builder.Services.AddScoped<IFollowUpJobRepository, FollowUpJobRepository>();
builder.Services.AddScoped<IFollowUpPolicyRepository, FollowUpPolicyRepository>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IAssignmentUserRepository, AssignmentUserRepository>();
builder.Services.AddScoped<ILeadAssignmentRepository, LeadAssignmentRepository>();
builder.Services.AddScoped<ILeadAssignmentService, LeadAssignmentService>();
builder.Services.AddSingleton<IAssignmentProtectionStore, InMemoryAssignmentProtectionStore>();
builder.Services.AddScoped<ILeadScoringService, LeadScoringService>();
builder.Services.AddSingleton<ILeadScoringFormulaStore, InMemoryLeadScoringFormulaStore>();
builder.Services.AddSingleton<ILeadPriorityThresholdStore, InMemoryLeadPriorityThresholdStore>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<AnalyticsAdvancedDataRepository>();
builder.Services.AddScoped<IAnalyticsAdvancedDataRepository>(sp =>    new CachedAnalyticsAdvancedDataRepository(        sp.GetRequiredService<AnalyticsAdvancedDataRepository>(),        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),        sp.GetRequiredService<ITenantContext>(),        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnalyticsAdvancedCacheOptions>>()));
builder.Services.AddScoped<IAnalyticsAdvancedService, AnalyticsAdvancedService>();
builder.Services.AddScoped<IAnalyticsCsvExportService, AnalyticsCsvExportService>();
builder.Services.AddScoped<IWeeklyAnalyticsReportService, WeeklyAnalyticsReportService>();
builder.Services.Configure<WeeklyAnalyticsReportOptions>(builder.Configuration.GetSection("WeeklyAnalyticsReports"));
builder.Services.AddSingleton<IAnalyticsObservabilityService, InMemoryAnalyticsObservabilityService>();
builder.Services.AddScoped<IObservabilitySnapshotRepository, ObservabilitySnapshotRepository>();
builder.Services.AddScoped<IObservabilityPersistenceService, ObservabilityPersistenceService>();
builder.Services.AddScoped<IObservabilityIncrementalAggregationService, ObservabilityIncrementalAggregationService>();
builder.Services.AddScoped<IAlertThresholdRepository, AlertThresholdRepository>();
builder.Services.AddScoped<IAlertEventRepository, AlertEventRepository>();
builder.Services.AddScoped<IPoisonQueueRemediationRunRepository, PoisonQueueRemediationRunRepository>();
builder.Services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();
builder.Services.AddScoped<IPoisonQueueAlertService, PoisonQueueAlertService>();
builder.Services.AddHostedService<ObservabilityPersistenceBackgroundService>();
builder.Services.AddHostedService<WeeklyAnalyticsReportBackgroundService>();
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRuleService, RuleEngineService>();
builder.Services.AddScoped<IRuleEventListener, RuleEngineService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOnboardingTaskRepository, OnboardingTaskRepository>();
builder.Services.AddScoped<IOnboardingWelcomeJobRepository, OnboardingWelcomeJobRepository>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
builder.Services.AddScoped<IProposalReminderJobRepository, ProposalReminderJobRepository>();
builder.Services.AddScoped<IProposalPdfGenerator, SimpleProposalPdfGenerator>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
// OPS-05 | Feature flags service (configuration-backed, tenant-aware)
builder.Services.AddSingleton<IFeatureFlagService, ConfigurationFeatureFlagService>();
var disableDataRetentionBackground = builder.Configuration.GetValue<bool>("Features:DisableDataRetentionBackground");
if (!disableDataRetentionBackground)
{
    builder.Services.AddHostedService<SensitiveDataRetentionService>();
}
builder.Services.AddScoped<ILeadScoringAIService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var aiServiceUrl = config["AI:LeadScoringServiceUrl"] ?? "http://localhost:5200";
    return new LeadScoringAIService(new HttpClient(), aiServiceUrl);
});

// Sequences
builder.Services.AddScoped<ISequenceRepository, SequenceRepository>();
builder.Services.AddScoped<ISequenceEnrollmentRepository, SequenceEnrollmentRepository>();
builder.Services.AddScoped<ISequenceService, SequenceService>();
builder.Services.AddScoped<ISequenceEngine, SequenceEngine>();
builder.Services.AddHostedService<SequenceEngineBackgroundService>();

// Lead Query (filter/sort by custom fields)
builder.Services.AddScoped<ILeadQueryService, Api.Infrastructure.Leads.LeadQueryService>();

// Custom Fields
builder.Services.AddScoped<ICustomFieldRepository, CustomFieldRepository>();
builder.Services.AddScoped<ICustomFieldService, CustomFieldService>();

// WhatsApp
builder.Services.AddScoped<IWhatsAppRepository, WhatsAppRepository>();
builder.Services.AddHttpClient<IWhatsAppOutboundService, WhatsAppOutboundService>();
builder.Services.AddScoped<WhatsAppService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
    if (app.Environment.IsDevelopment())
    {
        // In dev, always recreate the schema from the current model (fast, no migration gaps).
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("NovamindCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<ApiVersioningMiddleware>();
app.UseMiddleware<LeadIntakeApiKeyMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<BruteForceProtectionMiddleware>();
app.UseMiddleware<RoleAuthorizationMiddleware>();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers().RequireRateLimiting("api-by-tenant-ip");
app.Run();
public partial class Program;
