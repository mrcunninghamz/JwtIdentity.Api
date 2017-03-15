using System.Threading.Tasks;

namespace JwtIdentity.Api.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
