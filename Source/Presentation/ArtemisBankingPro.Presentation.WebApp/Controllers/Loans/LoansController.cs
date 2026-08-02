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
        //Listado principal: paginado, con búsqueda por cédula y filtro de estado.
        [HttpGet]
        public async Task<IActionResult> Index(LoansFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                return View(BuildList(filter, PagedResult<LoansDto>.Empty(1, DomainConstants.DefaultPageSize)));
            }

            var result = await _loansServices.GetPagedLoansAsync(_mapper.Map<LoansFilterDto>(filter));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(BuildList(filter, PagedResult<LoansDto>.Empty(filter.Page, DomainConstants.DefaultPageSize)));
            }

            return View(BuildList(filter, result.Value!));
        }

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
        [HttpGet]
        public IActionResult Create(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignacion de prestamo sin cliente seleccionado");
                return RedirectToAction(nameof(Index));
            }

            return View(new LoansAssigmentViewModel
            {
                CustomerId = customerId,
                TermLoans = TermMonths.Meses6,
                AmmountLoans = 0m,
                AnnualInterestRate = 0m
            });
        }
        [HttpPost]
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

        [HttpPost]
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
