using ArtemisBankingPro.Core.Application.ViewModels.Atm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class AtmController : Controller
    {
        // TODO: Inject IAccountServices and ITransactionServices when they are available from the Dev branch

        public AtmController()
        {
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Call TransactionService to get dashboard indicators for the cashier
            // var indicators = await _transactionServices.GetAtmDashboardIndicatorsAsync(userId);
            
            // Mocking for now to prevent view crash
            var indicators = new AtmDashboardViewModel();
            return View(indicators);
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View(new DepositViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Validate account with IAccountServices
            // bool isActive = await _accountServices.IsAccountActiveAsync(model.DestinationAccountNumber);
            bool isActive = true; // Placeholder

            if (!isActive)
            {
                ModelState.AddModelError("DestinationAccountNumber", "The account number entered does not correspond to a valid account.");
                return View(model);
            }

            // TODO: Get account holder name with IAccountServices
            // var accountHolderName = await _accountServices.GetAccountHolderNameAsync(model.DestinationAccountNumber);
            var accountHolderName = "Placeholder Name"; 

            var confirmationModel = new DepositConfirmationViewModel
            {
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount,
                AccountHolderName = accountHolderName
            };

            return View("ConfirmDeposit", confirmationModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmDeposit(DepositConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Process deposit with ITransactionServices
            // bool success = await _transactionServices.ProcessDepositAsync(model, userId);
            bool success = true; // Placeholder

            if (!success)
            {
                ModelState.AddModelError("", "An error occurred while processing the deposit.");
                return View(model);
            }

            // TODO: Send email notification via IEmailServices

            return RedirectToAction("Index");
        }
    }
}
