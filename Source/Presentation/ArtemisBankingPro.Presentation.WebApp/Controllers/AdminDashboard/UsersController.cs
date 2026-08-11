using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.AdminDashboard
{
    [Authorize(Roles = "Administrador")]
    public class UsersController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IAccountRegistrationService _accountRegistrationService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserManagementService userManagementService,
            IAccountRegistrationService accountRegistrationService,
            ILogger<UsersController> logger)
        {
            _userManagementService = userManagementService;
            _accountRegistrationService = accountRegistrationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Roles? role = null, StatusFilter status = StatusFilter.Todos, int page = 1)
        {
            const int pageSize = 20;
            
            ViewBag.CurrentRole = role;
            ViewBag.CurrentStatus = status;
            ViewBag.Roles = await _userManagementService.GetRolesAsync();

            if (role.HasValue)
            {
                var pagedUsersByRole = await _userManagementService.GetUsersByRoleAsync(role.Value, page, pageSize);
                return View(pagedUsersByRole);
            }

            var pagedUsers = await _userManagementService.GetUsersAsync(page, pageSize, status);
            return View(pagedUsers);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _userManagementService.GetRolesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _userManagementService.GetRolesAsync();
                return View(request);
            }

            // El origen es necesario para el enlace de activacion de la WebApp
            request.Origin = $"{Request.Scheme}://{Request.Host.Value}";

            var response = await _accountRegistrationService.RegisterUserAsync(request);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error ?? "Ha ocurrido un error al registrar el usuario.");
                ViewBag.Roles = await _userManagementService.GetRolesAsync();
                return View(request);
            }

            TempData["SuccessMessage"] = "Usuario creado exitosamente. Se ha enviado un correo de activación.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "El usuario seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            var editUserDto = new EditUserDto
            {
                Id = user.Id ?? string.Empty,
                Name = user.Name,
                LastName = user.LastName,
                IDCARD = user.IDCARD,
                Email = user.Email,
                UserName = user.UserName,
                AdditionalAmount = 0
            };

            ViewBag.IsClient = user.IsClient;
            return View(editUserDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                ViewBag.IsClient = user?.IsClient ?? false;
                return View(dto);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            var response = await _userManagementService.UpdateUserAsync(id, dto);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error ?? "Ha ocurrido un error al actualizar el usuario.");
                var user = await _userManagementService.GetUserByIdAsync(id);
                ViewBag.IsClient = user?.IsClient ?? false;
                return View(dto);
            }

            TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "No puede modificar el estado de su propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var response = await _userManagementService.ToggleUserAsync(id, currentUserId);

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
    }
}
