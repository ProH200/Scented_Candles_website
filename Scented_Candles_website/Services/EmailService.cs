using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace ScentedCandleWebsite.Services
{
    public class EmailService:IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        // ✅ Implement SendPasswordResetEmailAsync
        public async Task SendPasswordResetEmailAsync(string email, string resetLink, string userName)
        {
            try
            {
                // Log to file for testing
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email-logs.txt");
                var content = $@"
========================================
Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
To: {email}
User: {userName}
Type: Password Reset
Reset Link: {resetLink}
========================================
";

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.AppendAllTextAsync(logPath, content);
                _logger.LogInformation("Password reset email logged for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log password reset email for {Email}", email);
                throw;
            }
        }

        // ✅ Implement SendEmailConfirmationAsync (THIS WAS MISSING)
        public async Task SendEmailConfirmationAsync(string email, string confirmationLink, string userName)
        {
            try
            {
                // Log to file for testing
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email-logs.txt");
                var content = $@"
========================================
Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
To: {email}
User: {userName}
Type: Email Confirmation
Confirmation Link: {confirmationLink}
========================================
";

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.AppendAllTextAsync(logPath, content);
                _logger.LogInformation("Email confirmation logged for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log email confirmation for {Email}", email);
                throw;
            }
        }
    }
}