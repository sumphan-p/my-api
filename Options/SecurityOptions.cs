namespace AuthAPI.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public int BcryptWorkFactor { get; set; } = 12;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int PasswordResetExpiryMinutes { get; set; } = 60;
}
