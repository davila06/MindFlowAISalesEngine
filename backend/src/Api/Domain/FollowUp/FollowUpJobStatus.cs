namespace Api.Domain.FollowUp;

public static class FollowUpJobStatus
{
    public const string Scheduled = "Scheduled";
    public const string Sent      = "Sent";
    public const string Failed    = "Failed";
    public const string Poisoned  = "Poisoned";
    public const string Cancelled = "Cancelled";
}
