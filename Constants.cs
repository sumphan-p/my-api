namespace AuthAPI;

public static class AuditActions
{
    public const string Login = "login";
    public const string Logout = "logout";
    public const string FailedLogin = "failed_login";
    public const string Register = "register";
    public const string ChangePassword = "change_password";
    public const string ForgotPassword = "forgot_password";
    public const string ResetPassword = "reset_password";
    public const string ClientCreated = "client_created";
    public const string ClientDisabled = "client_disabled";
}

public static class CookieNames
{
    public const string RefreshToken = "refresh_token";
}

public static class HttpHeaderNames
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string ClientId = "X-Client-Id";
    public const string ClientSecret = "X-Client-Secret";
}
