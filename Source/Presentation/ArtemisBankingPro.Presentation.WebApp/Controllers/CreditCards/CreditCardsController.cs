using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.CreditCards
{
    //Gestión de tarjetas de crédito: solo el rol Administrador accede al módulo.
    //Todo formulario viaja por POST con token antifalsificación.
    [Authorize(Roles = "Administrador")]
    public sealed class CreditCardsController : Controller
    {
        private readonly ICreditCardsServices _creditCardsServices;
        private readonly IMapper _mapper;
        private readonly ILogger<CreditCardsController> _logger;

        public CreditCardsController(
            ICreditCardsServices creditCardsServices,
            IMapper mapper,
            ILogger<CreditCardsController> logger)
        {
            _creditCardsServices = creditCardsServices;
            _mapper = mapper;
            _logger = logger;
        }

        #region listado y detalles
        //Listado principal: carga inicial, paginación y regreso desde las demás pantallas.
        [HttpGet]
        public async Task<IActionResult> Index(CreditCardFilterViewModel filter)
            => await ListCreditCardsAsync(filter);

        //Formulario de búsqueda por cédula y filtro de estado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Index))]
        public async Task<IActionResult> IndexFilter(CreditCardFilterViewModel filter)
            => await ListCreditCardsAsync(filter);

        //Ver detalles: datos de la tarjeta y su historial de consumos paginado.
        [HttpGet]
        public async Task<IActionResult> Details(int id, int page = 1)
        {
            var creditCard = await _creditCardsServices.GetByIdAsync(id);

            if (!creditCard.IsValid)
            {
                _logger.LogWarning("No fue posible mostrar los detalles de la tarjeta con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            var consumptions = await _creditCardsServices.GetPagedConsumptionsAsync(id, page);

            if (!consumptions.IsValid)
            {
                AddErrors(consumptions);
                return View(BuildConsumptions(
                    creditCard.Value!, PagedResult<CardConsumptionDto>.Empty(page, DomainConstants.DefaultPageSize)));
            }

            return View(BuildConsumptions(creditCard.Value!, consumptions.Value!));
        }
        #endregion

        #region asignar tarjeta
        //Paso 1: llega el cliente elegido en el formulario de selección y se muestra el paso 2.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectCustomer(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignacion de tarjeta de credito sin cliente seleccionado");
                return RedirectToAction(nameof(Index));
            }

            return View("Create", new CreditCardAssignmentViewModel
            {
                CustomerId = customerId,
                CreditLimit = 0m
            });
        }

        //Paso 2: envío del formulario de configuración del límite de crédito.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditCardAssignmentViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _creditCardsServices.AssignCreditCardAsync(
                _mapper.Map<CreditCardAssignmentDto>(vm));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(vm);
            }

            //El correo de asignación se envía desde el servicio cuando Identity exponga el
            //nombre y el correo del cliente; su fallo no revierte la tarjeta creada.

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region editar limite de credito
        //Acción Editar del listado: precarga el formulario con el límite actual.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _creditCardsServices.GetCreditCardForEditLimitAsync(id);

            if (!result.IsValid)
            {
                _logger.LogWarning("No fue posible cargar la edicion de limite de la tarjeta con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<EditCardLimitViewModel>(result.Value!));
        }

        //Envío del formulario de modificación del límite.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCardLimitViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _creditCardsServices.EditCreditCardLimitAsync(
                _mapper.Map<EditCardLimitDto>(vm));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(vm);
            }

            //El correo de modificación de límite se envía desde el servicio cuando Identity
            //exponga el nombre y el correo del cliente; su fallo no revierte el nuevo límite.

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region cancelar tarjeta
        //Acción Cancelar del listado: pantalla de confirmación con los últimos 4 dígitos.
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _creditCardsServices.GetByIdAsync(id);

            if (!result.IsValid)
            {
                _logger.LogWarning("No fue posible cargar la cancelacion de la tarjeta con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            if (result.Value!.StateCreditCard != CreditCardStatus.Activa)
            {
                _logger.LogWarning("Intento de cancelar la tarjeta con ID {ID} que ya se encuentra cancelada", id);
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<CancelCreditCardViewModel>(result.Value!));
        }

        //Confirmación de la cancelación: el servicio rechaza la tarjeta con deuda pendiente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(CancelCreditCardViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _creditCardsServices.CancelCreditCardAsync(vm.CreditCardId);

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
        private async Task<IActionResult> ListCreditCardsAsync(CreditCardFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Index),
                    BuildList(filter, PagedResult<CreditCardDto>.Empty(1, DomainConstants.DefaultPageSize)));
            }

            //La cédula del filtro identifica al cliente dentro del project Identity. Mientras el
            //servicio de consulta de usuarios no esté disponible, el listado no puede traducirla
            //y la búsqueda por cédula queda sin efecto.
            //var customer = await _userServices.GetCustomerByIdCardAsync(filter.IdCard);
            //if (customer is null) -> CreditCardError.NonExistsCustomerByIdCard
            string? customerId = null;

            var result = await _creditCardsServices.GetPagedCreditCardsAsync(
                _mapper.Map<CreditCardFilterDto>(filter), customerId);

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(nameof(Index),
                    BuildList(filter, PagedResult<CreditCardDto>.Empty(filter.Page, DomainConstants.DefaultPageSize)));
            }

            return View(nameof(Index), BuildList(filter, result.Value!));
        }

        private CreditCardsListViewModel BuildList(
            CreditCardFilterViewModel filter, PagedResult<CreditCardDto> paged)
            => new()
            {
                Filter = filter,
                CreditCards = _mapper.Map<IReadOnlyCollection<CreditCardViewModel>>(paged.Items),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalRecords = paged.TotalRecords,
                TotalPages = paged.TotalPages
            };

        private ConsumptionListViewModel BuildConsumptions(
            CreditCardDto creditCard, PagedResult<CardConsumptionDto> paged)
            => new()
            {
                CreditCard = _mapper.Map<DetailsCreditCardViewModel>(creditCard),
                Consumptions = _mapper.Map<IReadOnlyCollection<CardConsumptionViewModel>>(paged.Items),
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
