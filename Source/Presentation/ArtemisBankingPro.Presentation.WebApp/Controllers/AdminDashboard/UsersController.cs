using Artemis_Banking_Pro.Core.Application.ViewModels.Users;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.AdminDashboard
{
   
    [Authorize(Roles = "Administrador")]
    public sealed class UsersController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IAccountRegistrationService _accountRegistrationService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserManagementService userManagementService,
            IAccountRegistrationService accountRegistrationService,
            IMapper mapper,
            ILogger<UsersController> logger)
        {
            _userManagementService = userManagementService;
            _accountRegistrationService = accountRegistrationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region listado
        //Listado principal: carga inicial, paginación y regreso desde las demás pantallas.
        [HttpGet]
        public async Task<IActionResult> Index(UsersFilterViewModel filter)
            => await ListUsersAsync(filter);

        //Formulario de filtros por tipo de usuario y estado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Index))]
        public async Task<IActionResult> IndexFilter(UsersFilterViewModel filter)
            => await ListUsersAsync(filter);
        #endregion

        #region crear usuario
        [HttpGet]
        public async Task<IActionResult> Create()
            => View(await BuildCreateFormAsync(EmptyCreateForm()));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveUserViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(await BuildCreateFormAsync(vm));

            var request = _mapper.Map<RegisterRequest>(vm);
            //El origen es necesario para el enlace de activacion de la WebApp
            request.Origin = $"{Request.Scheme}://{Request.Host.Value}";

            var response = await _accountRegistrationService.RegisterUserAsync(request);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error ?? "Ha ocurrido un error al registrar el usuario.");
                return View(await BuildCreateFormAsync(vm));
            }

            TempData["SuccessMessage"] = "Usuario creado exitosamente. Se ha enviado un correo de activación.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region editar usuario
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            //El servicio decide si el usuario puede editarse: existencia, rol y cuenta propia
            var response = await _userManagementService.GetUserForEditAsync(id);

            if (response.HasError)
            {
                _logger.LogWarning("No fue posible cargar la edicion del usuario con Id {UserId}", id);
                TempData["ErrorMessage"] = response.Error;
                return RedirectToAction(nameof(Index));
            }

            return View(_mapper.Map<EditUserViewModel>(response.User!));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(await RestoreClientFlagAsync(vm));

            var response = await _userManagementService.UpdateUserAsync(vm.Id!, _mapper.Map<EditUserDto>(vm));

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error ?? "Ha ocurrido un error al actualizar el usuario.");
                return View(await RestoreClientFlagAsync(vm));
            }

            TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region activar / inactivar usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            //La cuenta propia la rechaza el servicio: aquí no se conoce al usuario autenticado
            var response = await _userManagementService.ToggleUserAsync(id);

            if (response.HasError)
            {
                TempData["ErrorMessage"] = response.Error ?? "Ha ocurrido un error al cambiar el estado del usuario.";
            }
            else
            {
                TempData["SuccessMessage"] = "Estado del usuario actualizado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region private methods
        //Comparten el mismo armado de pantalla la carga inicial y el envío del filtro.
        private async Task<IActionResult> ListUsersAsync(UsersFilterViewModel filter)
        {
            var roles = await _userManagementService.GetRolesAsync();

            if (!ModelState.IsValid)
                return View(nameof(Index), BuildList(filter, roles, EmptyPage(filter)));

            //El tamaño de página lo fija el filtro: el servicio vuelve a acotarlo al máximo
            var paged = filter.Role.HasValue
                ? await _userManagementService.GetUsersByRoleAsync(
                    filter.Role.Value, filter.Page, filter.PageSize)
                : await _userManagementService.GetUsersAsync(
                    filter.Page, filter.PageSize, filter.Status);

            return View(nameof(Index), BuildList(filter, roles, paged));
        }

        private UsersListViewModel BuildList(
            UsersFilterViewModel filter, List<string> roles, PagedResponseDto<UserDto> paged)
            => new()
            {
                Filter = filter,
                AvailableRoles = roles,
                Users = _mapper.Map<IReadOnlyCollection<UserViewModel>>(paged.Items),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalRecords = paged.TotalCount,
                TotalPages = paged.TotalPages
            };

        //El combo de tipos de usuario se repuebla en cada carga del formulario de creación
        private async Task<SaveUserViewModel> BuildCreateFormAsync(SaveUserViewModel vm)
        {
            vm.AvailableRoles = await _userManagementService.GetRolesAsync();
            return vm;
        }

        //El indicador de cliente no viaja en el formulario: decide si se pide monto adicional
        private async Task<EditUserViewModel> RestoreClientFlagAsync(EditUserViewModel vm)
        {
            var user = await _userManagementService.GetUserByIdAsync(vm.Id ?? string.Empty);
            vm.IsClient = user?.IsClient ?? false;
            return vm;
        }

        private static SaveUserViewModel EmptyCreateForm()
            => new()
            {
                FirstName = string.Empty,
                LastName = string.Empty,
                IDCARD = string.Empty,
                Email = string.Empty,
                UserName = string.Empty,
                Password = string.Empty,
                ConfirmPassword = string.Empty,
                Role = string.Empty
            };

        //Con un filtro invalido la pantalla se repinta vacía, nunca sin listado
        private static PagedResponseDto<UserDto> EmptyPage(UsersFilterViewModel filter)
            => new()
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                Page = filter.Page < 1 ? 1 : filter.Page,
                PageSize = filter.PageSize
            };
        #endregion
    }
}
