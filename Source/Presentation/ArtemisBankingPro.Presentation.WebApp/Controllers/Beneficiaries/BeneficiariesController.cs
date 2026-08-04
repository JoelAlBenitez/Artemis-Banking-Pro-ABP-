using System.Security.Claims;
using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.ViewModels.Beneficiaries;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Beneficiaries
{
    [Authorize(Roles = "Cliente")]
    public class BeneficiariesController : Controller
    {
        private readonly IBeneficiaryServices _beneficiaryServices;
        private readonly IMapper _mapper;

        public BeneficiariesController(
            IBeneficiaryServices beneficiaryServices,
            IMapper mapper)
        {
            _beneficiaryServices = beneficiaryServices;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _beneficiaryServices.GetClientBeneficiariesAsync(clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = "No se pudieron cargar los beneficiarios.";
                return View(new List<BeneficiaryListViewModel>());
            }

            var viewModels = result.Value!.Select(d => new BeneficiaryListViewModel
            {
                Id = d.Id,
                Name = d.OwnerFullName.StartsWith("Cliente ") ? d.OwnerFullName : "Cliente",
                LastName = d.OwnerFullName.StartsWith("Cliente ") ? d.OwnerFullName.Replace("Cliente ", "") : "Asociado",
                AccountNumber = d.AccountNumber
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View(new SaveBeneficiaryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(SaveBeneficiaryViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var dto = _mapper.Map<SaveBeneficiaryDto>(vm);
            dto.OwnerClientId = clientId;

            var result = await _beneficiaryServices.CreateAsync(dto);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(vm);
            }

            TempData["SuccessMessage"] = "Beneficiario agregado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _beneficiaryServices.DeactivateAsync(id, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el beneficiario.";
            }
            else
            {
                TempData["SuccessMessage"] = "Beneficiario eliminado correctamente.";
            }

            return RedirectToAction("Index");
        }
    }
}
