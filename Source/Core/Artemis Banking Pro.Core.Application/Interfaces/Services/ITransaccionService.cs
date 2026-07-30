using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services
{
    public interface ITransaccionService
    {
        Task RegistrarTransaccionAsync(TransaccionDto transaccion);
    }
}
