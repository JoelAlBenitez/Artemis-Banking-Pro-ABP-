using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Customers
{
    [Authorize(Roles = "Cliente")]
    public class CustomerController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public CustomerController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _dashboardService.GetClientDashboardAsync(clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description;
                return View(new Artemis_Banking_Pro.Core.Application.ViewModels.Dashboard.ClientDashboardViewModel());
            }

            return View(result.Value);
        }

        public async Task<IActionResult> AccountDetails(int id)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _dashboardService.GetSavingsAccountDetailsAsync(id, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }

        public async Task<IActionResult> CardDetails(int id)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _dashboardService.GetCreditCardDetailsAsync(id, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }

        public async Task<IActionResult> LoanDetails(int id)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _dashboardService.GetLoanDetailsAsync(id, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }
    }
}
