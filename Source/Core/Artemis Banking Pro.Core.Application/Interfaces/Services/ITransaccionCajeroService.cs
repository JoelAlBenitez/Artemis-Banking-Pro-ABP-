using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services
{
    public interface ITransaccionCajeroService
    {
        Task<DashboardCajeroViewModel> ObtenerIndicadoresHoyAsync(string userId);
    }
}
