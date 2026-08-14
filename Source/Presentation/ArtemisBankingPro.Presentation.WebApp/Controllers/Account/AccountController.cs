using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.ViewModels.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Account
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAuthWebAppService _authService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private readonly IAccountRegistrationService _accountRegistrationService;

        public AccountController(
            IAuthWebAppService authService,
            IPasswordRecoveryService passwordRecoveryService,
            IAccountRegistrationService accountRegistrationService)
        {
            _authService = authService;
            _passwordRecoveryService = passwordRecoveryService;
            _accountRegistrationService = accountRegistrationService;
        }

        // ─── LOGIN ───────────────────────────────────────────────────────────

        public IActionResult Login()
        {
            //El Login solo está disponible para usuarios no autenticados
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToRoleHome();

            if (Request.Query.ContainsKey("expired"))
                ModelState.AddModelError(string.Empty, "Su sesión ha expirado. Inicie sesión nuevamente.");

            return View(new AuthenticationRequest { UserName = "", Password = "" });
        }

        [HttpPost]
        public async Task<IActionResult> Login(AuthenticationRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var response = await _authService.LoginAsync(request);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error!);
                return View(request);
            }

            return RedirectToRoleHome(response.Roles);
        }

        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction(nameof(Login));
        }

        // ─── RESTABLECIMIENTO DE CONTRASEÑA ──────────────────────────────────

        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordRequest { UserName = "" });
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var origin = $"{Request.Scheme}://{Request.Host.Value}";
            var response = await _passwordRecoveryService.ForgotPasswordAsync(request, origin);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error!);
                return View(request);
            }

            ViewBag.Message = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
            return View(request);
        }

        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordRequest
            {
                Token = token,
                Email = email,
                Password = "",
                ConfirmPassword = ""
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var response = await _passwordRecoveryService.ResetPasswordAsync(request);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Error!);
                return View(request);
            }

            //Tras restablecer correctamente, el usuario vuelve al Login ya activo
            TempData["Message"] = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        // ─── ACTIVACIÓN DE CUENTA ────────────────────────────────────────────

        public async Task<IActionResult> ConfirmAccountEmail(string userId, string token)
        {
            var response = await _accountRegistrationService.ConfirmAccountAsync(userId, token);

            //Una activación correcta envía al Login con el mensaje de confirmación
            if (!response.HasError)
            {
                TempData["Message"] = response.Message;
                return RedirectToAction(nameof(Login));
            }

            return View("ConfirmEmail", response.Message);
        }

        // ─── ACCESO DENEGADO ─────────────────────────────────────────────────

        public IActionResult AccessDenied()
        {
            var homeController = "Account";
            var homeAction = "Login";

            if (User.IsInRole(nameof(Roles.Administrador)))
            {
                homeController = "AdminHome";
                homeAction = "Index";
            }
            else if (User.IsInRole(nameof(Roles.Cajero)))
            {
                homeController = "CashierHome";
                homeAction = "Index";
            }
            else if (User.IsInRole(nameof(Roles.Cliente)))
            {
                homeController = "ClientHome";
                homeAction = "Index";
            }

            return View(new AccessDeniedViewModel
            {
                HomeController = homeController,
                HomeAction = homeAction
            });
        }

        // ─── HELPER ──────────────────────────────────────────────────────────

        private IActionResult RedirectToRoleHome(List<string>? roles = null)
        {
            var userRoles = roles ?? User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            //El nombre del controlador, no el de la pantalla: AdminController expone el Home
            //del administrador. "AdminHome" no existe y la redirección terminaba en 404.
            if (userRoles.Contains(nameof(Roles.Administrador)))
                return RedirectToAction("Index", "Admin");

            //Pendiente: el módulo Cajero todavía no tiene su controlador en la WebApp
            if (userRoles.Contains(nameof(Roles.Cajero)))
                return RedirectToAction("Index", "CashierHome");

            if (userRoles.Contains(nameof(Roles.Cliente)))
                return RedirectToAction("Index", "Customer");

            //El rol Comercio no tiene Home en la aplicación web: solo consume la Web API
            return RedirectToAction(nameof(Login));
        }
    }
}
