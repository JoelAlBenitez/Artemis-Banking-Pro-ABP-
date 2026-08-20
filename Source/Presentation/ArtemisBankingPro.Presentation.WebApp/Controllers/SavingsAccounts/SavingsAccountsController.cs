using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.SavingsAccounts
{
   
    [Authorize(Roles = "Administrador")]
    public sealed class SavingsAccountsController : Controller
    {
        private readonly ISavingsAccountsServices _savingsAccountsServices;
        private readonly IMapper _mapper;
        private readonly ILogger<SavingsAccountsController> _logger;

        public SavingsAccountsController(
            ISavingsAccountsServices savingsAccountsServices,
            IMapper mapper,
            ILogger<SavingsAccountsController> logger)
        {
            _savingsAccountsServices = savingsAccountsServices;
            _mapper = mapper;
            _logger = logger;
        }

        #region listado y detalles
        //Listado principal: carga inicial, paginación y regreso desde las demás pantallas.
        [HttpGet]
        public async Task<IActionResult> Index(SavingsAccountFilterViewModel filter)
            => await ListSavingsAccountsAsync(filter);

        //Formulario de búsqueda por cédula y filtros de estado y tipo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Index))]
        public async Task<IActionResult> IndexFilter(SavingsAccountFilterViewModel filter)
            => await ListSavingsAccountsAsync(filter);

        //Ver detalles: datos de la cuenta y su historial de transacciones paginado.
        [HttpGet]
        public async Task<IActionResult> Details(int id, int page = 1)
        {
            var savingsAccount = await _savingsAccountsServices.GetByIdAsync(id);

            if (!savingsAccount.IsValid)
            {
                _logger.LogWarning("No fue posible mostrar los detalles de la cuenta de ahorro con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            var transactions = await _savingsAccountsServices.GetPagedTransactionsAsync(id, page);

            if (!transactions.IsValid)
            {
                AddErrors(transactions);
                return View(BuildTransactions(
                    savingsAccount.Value!, PagedResult<TransactionDto>.Empty(page, DomainConstants.DefaultPageSize)));
            }

            return View(BuildTransactions(savingsAccount.Value!, transactions.Value!));
        }
        #endregion

        #region asignar cuenta secundaria
        //Paso 1: listado de clientes activos con su deuda total y búsqueda por cédula.
        [HttpGet]
        public async Task<IActionResult> Assign(string? idCard = null)
        {
            var result = await _savingsAccountsServices.GetActiveClientsAsync(idCard);

            if (!result.IsValid)
            {
                AddErrors(result);

                return View(new ClientsForSavingsAccountAssignmentViewModel
                {
                    Clients = Array.Empty<ClientSavingsAccountViewModel>(),
                    IdCard = idCard
                });
            }

            return View(new ClientsForSavingsAccountAssignmentViewModel
            {
                Clients = _mapper.Map<IReadOnlyCollection<ClientSavingsAccountViewModel>>(result.Value!),
                IdCard = idCard
            });
        }

        //Paso 1: llega el cliente elegido en el formulario de selección y se muestra el paso 2.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectCustomer(string customerId, string? fullNameCustomer = null)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignacion de cuenta de ahorro sin cliente seleccionado");
                return RedirectToAction(nameof(Assign));
            }

            return View("Create", new SavingsAccountAssignmentViewModel
            {
                CustomerId = customerId,
                InitialBalance = 0m,
                FullNameCustomer = fullNameCustomer
            });
        }

        //Paso 2: envío del formulario de configuración del balance inicial.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SavingsAccountAssignmentViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _savingsAccountsServices.AssignSavingsAccountAsync(
                _mapper.Map<SavingsAccountAssignmentDto>(vm));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region cancelar cuenta secundaria
        //Acción Cancelar del listado: pantalla de confirmación con el número de 9 dígitos.
        //Las cuentas principales no llegan aquí ni por acceso directo por URL.
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _savingsAccountsServices.GetByIdAsync(id);

            if (!result.IsValid)
            {
                _logger.LogWarning("No fue posible cargar la cancelacion de la cuenta de ahorro con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            if (result.Value!.StateSavingsAccount != SavingsAccountStatus.Activa)
            {
                _logger.LogWarning("Intento de cancelar la cuenta de ahorro con ID {ID} que ya se encuentra cancelada", id);
                return RedirectToAction(nameof(Index));
            }

            if (result.Value!.TypeSavingsAccount == SavingsAccountType.Principal)
            {
                _logger.LogWarning("Intento de cancelar por URL la cuenta principal con ID {ID}", id);
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<CancelSavingsAccountViewModel>(result.Value!));
        }

        //Confirmación de la cancelación: el servicio transfiere el balance remanente a la
        //cuenta principal antes de marcar la secundaria como Cancelada.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(CancelSavingsAccountViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _savingsAccountsServices.CancelSavingsAccountAsync(vm.SavingsAccountId);

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
        private async Task<IActionResult> ListSavingsAccountsAsync(SavingsAccountFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Index),
                    BuildList(filter, PagedResult<SavingsAccountDto>.Empty(1, DomainConstants.DefaultPageSize)));
            }

            //La cédula del filtro la traduce el servicio al Id del cliente en Identity
            var result = await _savingsAccountsServices.GetPagedSavingsAccountsAsync(
                _mapper.Map<SavingsAccountFilterDto>(filter));

            if (!result.IsValid)
            {
                AddErrors(result);
                return View(nameof(Index),
                    BuildList(filter, PagedResult<SavingsAccountDto>.Empty(filter.Page, DomainConstants.DefaultPageSize)));
            }

            return View(nameof(Index), BuildList(filter, result.Value!));
        }

        private SavingsAccountsListViewModel BuildList(
            SavingsAccountFilterViewModel filter, PagedResult<SavingsAccountDto> paged)
            => new()
            {
                Filter = filter,
                SavingsAccounts = _mapper.Map<IReadOnlyCollection<SavingsAccountViewModel>>(paged.Items),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalRecords = paged.TotalRecords,
                TotalPages = paged.TotalPages
            };

        private TransactionListViewModel BuildTransactions(
            SavingsAccountDto savingsAccount, PagedResult<TransactionDto> paged)
            => new()
            {
                SavingsAccount = _mapper.Map<DetailsSavingsAccountViewModel>(savingsAccount),
                Transactions = _mapper.Map<IReadOnlyCollection<TransactionViewModel>>(paged.Items),
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
