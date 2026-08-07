using Artemis_Banking_Pro.Core.Application.Contracts.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.ViewModels.AdminDashboard;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.AdminDashboard
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly IAdminDashboardServices _adminDashboardServices;
        private readonly IMapper _mapper;

        public AdminController(
         IAdminDashboardServices adminDashboardServices,
         IMapper mapper   
         ) { 
            
            _adminDashboardServices = adminDashboardServices;
            _mapper = mapper;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ///agregar ICurrentUserServices 
            var data = await _adminDashboardServices.GetDataAdminDashboard();
            var map = _mapper.Map<AdminDashboardViewModel>(data);
            return View(map);
        }
    }
}
