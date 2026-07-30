using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class CajeroController : Controller
    {
        private readonly ICuentaAhorroService _cuentaAhorroService;
        private readonly ITransaccionService _transaccionService;
        private readonly IEmailService _emailService;

        public CajeroController(
            ICuentaAhorroService cuentaAhorroService,
            ITransaccionService transaccionService,
            IEmailService emailService)
        {
            _cuentaAhorroService = cuentaAhorroService;
            _transaccionService = transaccionService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Deposito()
        {
            return View(new DepositoViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Deposito(DepositoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto a depositar debe ser mayor que cero.");
                return View(model);
            }

            var cuenta = await _cuentaAhorroService.ObtenerCuentaActivaPorNumeroAsync(model.NumeroCuentaDestino);
            if (cuenta == null || !cuenta.EstaActiva)
            {
                ModelState.AddModelError("NumeroCuentaDestino", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            return RedirectToAction(nameof(ConfirmarDeposito), new { cuenta = cuenta.NumeroCuenta, monto = model.Monto });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarDeposito(string cuenta, decimal monto)
        {
            var infoCuenta = await _cuentaAhorroService.ObtenerCuentaActivaPorNumeroAsync(cuenta);
            if (infoCuenta == null) return RedirectToAction(nameof(Deposito));

            var model = new ConfirmacionDepositoViewModel
            {
                NumeroCuentaDestino = cuenta,
                Monto = monto,
                NombreTitular = infoCuenta.NombreTitular
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarDeposito(ConfirmacionDepositoViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            await _cuentaAhorroService.ActualizarBalanceAsync(model.NumeroCuentaDestino, model.Monto);

            var transaccion = new TransaccionDto
            {
                TipoTransaccion = "CRÉDITO",
                Monto = model.Monto,
                Origen = "DEPÓSITO",
                Beneficiario = model.NumeroCuentaDestino,
                Estado = "APROBADA",
                UsuarioResponsable = userId,
                Fecha = DateTime.Now
            };
            await _transaccionService.RegistrarTransaccionAsync(transaccion);

            try
            {
                var ultimosCuatro = model.NumeroCuentaDestino.Length >= 4 
                    ? model.NumeroCuentaDestino.Substring(model.NumeroCuentaDestino.Length - 4) 
                    : model.NumeroCuentaDestino;
                    
                var asunto = $"Depósito realizado a su cuenta {ultimosCuatro}";
                var cuerpo = $@"Hola {model.NombreTitular},
Se ha realizado un depósito a su cuenta terminada en {ultimosCuatro}.
Monto depositado: RD${model.Monto}
Fecha y hora: {DateTime.Now}
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

                await _emailService.EnviarCorreoAsync("cliente@example.com", asunto, cuerpo);
            }
            catch (Exception)
            {
                TempData["InfoMessage"] = "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Depósito realizado correctamente.";
            return RedirectToAction("Index"); 
        }
    }
}
