namespace AuthAPI.Options;

public class AuthRateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 5;
    public int WindowMinutes { get; set; } = 1;
}
