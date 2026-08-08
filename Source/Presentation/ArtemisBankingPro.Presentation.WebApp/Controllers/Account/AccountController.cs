using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Presentation.WebApp.ViewModels;
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
            // Punto 4: usuario ya autenticado → redirigir a su Home real según rol
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToRoleHome();

            // Punto 6: mensajes para acceso denegado / sesión expirada
            if (Request.Query.ContainsKey("denied"))
                ModelState.AddModelError(string.Empty, "No tiene permiso para acceder a esta sección.");
            else if (Request.Query.ContainsKey("expired"))
                ModelState.AddModelError(string.Empty, "Su sesión ha expirado. Por favor inicie sesión nuevamente.");

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

            // Punto 3: post-login redirigir al Home del rol
            return RedirectToRoleHome(response.Roles);
        }

        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction(nameof(Login));
        }

        // ─── FORGOT / RESET PASSWORD ─────────────────────────────────────────

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

            ViewBag.Message = "Su contraseña ha sido restablecida exitosamente. Ya puede iniciar sesión.";
            return View(request);
        }

        // ─── CONFIRM EMAIL ───────────────────────────────────────────────────

        public async Task<IActionResult> ConfirmAccountEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            string response = await _accountRegistrationService.ConfirmAccountAsync(userId, token);
            return View("ConfirmEmail", response);
        }

        // ─── ACCESS DENIED ───────────────────────────────────────────────────

        public IActionResult AccessDenied()
        {
            var homeController = "Account";
            var homeAction = "Login";

            // Punto 2: controllers reales por rol
            if (User.IsInRole(Roles.Administrador.ToString()))
            {
                homeController = "AdminHome";
                homeAction = "Index";
            }
            else if (User.IsInRole(Roles.Cajero.ToString()))
            {
                homeController = "CashierHome";
                homeAction = "Index";
            }
            else if (User.IsInRole(Roles.Cliente.ToString()))
            {
                homeController = "ClientHome";
                homeAction = "Index";
            }

            return View(new AccessDeniedViewModel
            {
                Message = "No posee permisos para acceder a esta sección.",
                HomeController = homeController,
                HomeAction = homeAction
            });
        }

        // ─── HELPER ──────────────────────────────────────────────────────────

        private IActionResult RedirectToRoleHome(List<string>? roles = null)
        {
            // Si no se pasan roles, leerlos del claim del usuario autenticado
            var userRoles = roles ?? User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (userRoles.Contains(Roles.Administrador.ToString()))
                return RedirectToAction("Index", "AdminHome");

            if (userRoles.Contains(Roles.Cajero.ToString()))
                return RedirectToAction("Index", "CashierHome");

            if (userRoles.Contains(Roles.Cliente.ToString()))
                return RedirectToAction("Index", "ClientHome");

            // Fallback si el rol no es reconocido (ej. Comercio — solo usa la API)
            return RedirectToAction(nameof(Login));
        }
    }
}

