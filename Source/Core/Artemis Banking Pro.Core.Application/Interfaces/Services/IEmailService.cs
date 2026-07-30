using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo);
    }
}
