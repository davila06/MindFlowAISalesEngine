namespace Api.Domain.Leads;

/// <summary>
/// Immutable activity event tied to a lead — auto-captured by domain event handlers
/// or written manually (notes). Acts as the activity timeline feed.
/// </summary>
public class LeadActivity
{
    public static class ActivityTypes
    {
        public const string NoteAdded       = "note_added";
        public const string EmailSent       = "email_sent";
        public const string StageChanged    = "stage_changed";
        public const string Assigned        = "assigned";
        public const string ScoreChanged    = "score_changed";
        public const string CallLogged      = "call_logged";
        public const string LeadCreated     = "lead_created";
        public const string WhatsAppSent    = "whatsapp_sent";
        public const string WhatsAppReceived= "whatsapp_received";
        public const string SequenceStepSent= "sequence_step_sent";
    }

    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    /// <summary>Optional foreign key reference (e.g. EmailLog.Id, Opportunity.Id).</summary>
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    /// <summary>Who triggered this activity: "system", "automation", or a user identifier.</summary>
    public string Actor { get; private set; } = "system";
    public DateTime OccurredAtUtc { get; private set; }

    private LeadActivity() { }

    public static LeadActivity Create(
        Guid leadId,
        string activityType,
        string? title = null,
        string? description = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string actor = "system")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityType);
        return new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            ActivityType = activityType,
            Title = title,
            Description = description,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            Actor = actor,
            OccurredAtUtc = DateTime.UtcNow
        };
    }
}
