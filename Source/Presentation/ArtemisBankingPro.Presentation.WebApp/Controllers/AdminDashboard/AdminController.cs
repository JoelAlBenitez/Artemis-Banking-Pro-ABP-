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
        private readonly ILogger<AdminController> _logger;

        public AdminController(
         IAdminDashboardServices adminDashboardServices,
         IMapper mapper,
         ILogger<AdminController> logger
         ) {

            _adminDashboardServices = adminDashboardServices;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _adminDashboardServices.GetDataAdminDashboard();

           
            if (!data.IsValid)
            {
                _logger.LogWarning("No fue posible calcular los indicadores del Home del administrador");

                foreach (var error in data.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(EmptyIndicators());
            }

            return View(_mapper.Map<AdminDashboardViewModel>(data.Value!));
        }

        private static AdminDashboardViewModel EmptyIndicators()
            => new()
            {
                TotalHistoricalTransactions = 0,
                DayTransactions = 0,
                TotalHistoricalPay = 0,
                DayPay = 0,
                CustomerActive = 0,
                CustomerInactive = 0,
                TotalFinancialProducts = 0,
                OutstandingLoans = 0,
                CreditCardActive = 0,
                SavingAccountActive = 0,
                AverageDebtAmountPerCustomer = 0m
            };
    }
}
