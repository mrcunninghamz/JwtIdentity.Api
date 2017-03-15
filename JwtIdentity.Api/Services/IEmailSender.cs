using System.Threading.Tasks;

namespace JwtIdentity.Api.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
