using System.Threading.Tasks;

namespace ScentedCandleWebsite.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetLink, string userName);
        Task SendEmailConfirmationAsync(string email, string confirmationLink, string userName);
    }
}