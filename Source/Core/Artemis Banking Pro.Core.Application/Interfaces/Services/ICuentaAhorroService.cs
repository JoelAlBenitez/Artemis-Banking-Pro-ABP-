using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services
{
    public interface ICuentaAhorroService
    {
        Task<CuentaAhorroDto> ObtenerCuentaActivaPorNumeroAsync(string numeroCuenta);
        Task ActualizarBalanceAsync(string numeroCuenta, decimal monto);
    }
}
