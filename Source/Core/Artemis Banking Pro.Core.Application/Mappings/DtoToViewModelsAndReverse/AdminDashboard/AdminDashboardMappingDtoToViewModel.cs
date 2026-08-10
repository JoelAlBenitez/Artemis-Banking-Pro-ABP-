using Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.ViewModels.AdminDashboard;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.AdminDashboard
{
    public sealed class AdminDashboardMappingDtoToViewModel : Profile
    {
        public AdminDashboardMappingDtoToViewModel()
        {
            CreateMap<AdminDashboardDto, AdminDashboardViewModel>();
        }
    }
}
