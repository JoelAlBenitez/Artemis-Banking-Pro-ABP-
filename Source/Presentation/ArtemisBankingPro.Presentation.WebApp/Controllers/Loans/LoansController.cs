using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.ViewModels.Loans;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Loans
{
    //Gestión de préstamos: solo el rol Administrador accede al módulo.
    //Todo formulario viaja por POST con token antifalsificación.
    [Authorize(Roles = "Administrador")]
    public sealed class LoansController : Controller
    {
        private readonly ILoansServices _loansServices;
        private readonly IMapper _mapper;
        private readonly ILogger<LoansController> _logger;

        public LoansController(
            ILoansServices loansServices,
            IMapper mapper,
            ILogger<LoansController> logger)
        {
            _loansServices = loansServices;
            _mapper = mapper;
            _logger = logger;
        }

        #region listado y detalles
        //Listado principal: carga inicial, paginación y regreso desde las demás pantallas.
        [HttpGet]
        public async Task<IActionResult> Index(LoansFilterViewModel filter)
            => await ListLoansAsync(filter);

        //Formulario de búsqueda por cédula y filtro de estado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Index))]
        public async Task<IActionResult> IndexFilter(LoansFilterViewModel filter)
            => await ListLoansAsync(filter);

        //Ver detalles: información general del préstamo y su tabla de amortización.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _loansServices.GetDetailLoanAsync(id);

            if (!result.IsValid)
            {
                _logger.LogWarning("No fue posible mostrar los detalles del prestamo con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<DetailsLoansViewModel>(result.Value!));
        }
        #endregion

        #region asignar prestamo
        //Paso 1: llega el cliente elegido en el formulario de selección y se muestra el paso 2.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectCustomer(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignacion de prestamo sin cliente seleccionado");
                return RedirectToAction(nameof(Index));
            }

            return View("Create", new LoansAssigmentViewModel
            {
                CustomerId = customerId,
                TermLoans = TermMonths.Meses6,
                AmmountLoans = 0m,
                AnnualInterestRate = 0m
            });
        }

        //Paso 2: envío del formulario de configuración del préstamo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoansAssigmentViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _loansServices.CreateAsync(_mapper.Map<LoansAssignmentDto>(vm));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region editar tasa de interes anual
        //Acción Editar del listado: precarga el formulario con la tasa actual.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _loansServices.GetLoanForEditRateAsync(id);

            if (!result.IsValid)
            {
                _logger.LogWarning("No fue posible cargar la edicion de tasa del prestamo con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<EditAnnualInterestRateViewModel>(result.Value!));
        }

        //Envío del formulario de modificación de tasa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAnnualInterestRateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _loansServices.EditAnnualInterestRateAsync(
                _mapper.Map<EditAnnualInterestRateDto>(vm));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region private methods
        //Comparten el mismo armado de pantalla la carga inicial y el envío del filtro.
        private async Task<IActionResult> ListLoansAsync(LoansFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Index),
                    BuildList(filter, PagedResult<LoansDto>.Empty(1, DomainConstants.DefaultPageSize)));
            }

            var result = await _loansServices.GetPagedLoansAsync(_mapper.Map<LoansFilterDto>(filter));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(nameof(Index),
                    BuildList(filter, PagedResult<LoansDto>.Empty(filter.Page, DomainConstants.DefaultPageSize)));
            }

            return View(nameof(Index), BuildList(filter, result.Value!));
        }

        private LoansListViewModel BuildList(LoansFilterViewModel filter, PagedResult<LoansDto> paged)
            => new()
            {
                Filter = filter,
                Loans = _mapper.Map<IReadOnlyCollection<LoansViewModel>>(paged.Items),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalRecords = paged.TotalRecords,
                TotalPages = paged.TotalPages
            };

        //Los mensajes del documento funcional viajan en los errores del ValidationResult
        private void AddErrors(ValidationResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        #endregion
    }
}
