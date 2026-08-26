using System.Threading.Tasks;

namespace BookManagement.Service.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
