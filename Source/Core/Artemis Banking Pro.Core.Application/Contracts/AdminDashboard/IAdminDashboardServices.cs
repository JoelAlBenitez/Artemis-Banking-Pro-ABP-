using Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.AdminDashboard
{
    public interface IAdminDashboardServices
    {
        Task<ValidationResult<AdminDashboardDto>> GetDataAdminDashboard();
    }
}
