namespace AuthAPI.Options;

public class TokenCleanupOptions
{
    public const string SectionName = "TokenCleanup";

    public int IntervalHours { get; set; } = 24;
    public int InitialDelaySeconds { get; set; } = 10;
    public int AuditLogRetentionDays { get; set; } = 90;
}
