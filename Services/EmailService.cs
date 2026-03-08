using System.Net;
using System.Net.Mail;
using System.Web;
using AuthAPI.Options;
using Microsoft.Extensions.Options;

namespace AuthAPI.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string to, string token, string username);
}

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> emailOptions, IOptions<SecurityOptions> securityOptions, ILogger<SmtpEmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _securityOptions = securityOptions.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string token, string username)
    {
        var safeUsername = HttpUtility.HtmlEncode(username);
        var safeToken = HttpUtility.HtmlEncode(token);
        var expiryMinutes = _securityOptions.PasswordResetExpiryMinutes;

        var subject = "Password Reset Request — AuthAPI";
        var body = $"""
            <h2>Password Reset</h2>
            <p>Hi <strong>{safeUsername}</strong>,</p>
            <p>We received a request to reset your password. Use the token below:</p>
            <p style="font-size:18px; font-weight:bold; padding:10px; background:#f5f5f5; border-radius:4px;">{safeToken}</p>
            <p>This token expires in <strong>{expiryMinutes} minutes</strong>.</p>
            <p>If you didn't request this, please ignore this email.</p>
            <hr/>
            <p style="color:#888; font-size:12px;">AuthAPI — Centralized Auth Server</p>
            """;

        return await SendEmailWithRetryAsync(to, subject, body);
    }

    private async Task<bool> SendEmailWithRetryAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_emailOptions.Username))
            throw new InvalidOperationException("Email:Username is not configured");
        if (string.IsNullOrEmpty(_emailOptions.Password))
            throw new InvalidOperationException("Email:Password is not configured");

        for (int attempt = 1; attempt <= _emailOptions.RetryCount; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_emailOptions.Host, _emailOptions.Port)
                {
                    Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password),
                    EnableSsl = true
                };

                var message = new MailMessage(_emailOptions.FromAddress, to, subject, htmlBody)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} on attempt {Attempt}", to, attempt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email send failed to {To}, attempt {Attempt}/{Max}", to, attempt, _emailOptions.RetryCount);

                if (attempt < _emailOptions.RetryCount)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError("All {Max} email attempts failed for {To}", _emailOptions.RetryCount, to);
        return false;
    }
}
