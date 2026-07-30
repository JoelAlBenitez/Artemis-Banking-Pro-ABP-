using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly IMapper _mapper;
        private readonly ILogger<CajeroController> _logger;

        public CajeroController(
            ICuentaAhorroService cuentaAhorroService,
            ITransaccionService transaccionService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<CajeroController> logger)
        {
            _cuentaAhorroService = cuentaAhorroService;
            _transaccionService = transaccionService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Retiro()
        {
            return View(new RetiroViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Retiro(RetiroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto a retirar debe ser mayor que cero.");
                return View(model);
            }

            var cuenta = await _cuentaAhorroService.ObtenerCuentaActivaPorNumeroAsync(model.NumeroCuentaOrigen);
            if (cuenta == null || !cuenta.EstaActiva)
            {
                ModelState.AddModelError("NumeroCuentaOrigen", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            if (model.Monto > cuenta.Balance)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogWarning("Intento de retiro rechazado por fondos insuficientes. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto solicitado: {Monto}", userId, model.NumeroCuentaOrigen, model.Monto);
                
                var transaccionRechazada = new TransaccionDto
                {
                    TipoTransaccion = "DÉBITO",
                    Monto = model.Monto,
                    Origen = model.NumeroCuentaOrigen,
                    Beneficiario = "RETIRO",
                    Estado = "RECHAZADO",
                    UsuarioResponsable = userId,
                    Fecha = DateTime.Now
                };
                await _transaccionService.RegistrarTransaccionAsync(transaccionRechazada);

                ModelState.AddModelError("Monto", "El monto ingresado excede el saldo disponible de la cuenta.");
                return View(model);
            }

            return RedirectToAction(nameof(ConfirmarRetiro), new { cuenta = cuenta.NumeroCuenta, monto = model.Monto });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarRetiro(string cuenta, decimal monto)
        {
            var infoCuenta = await _cuentaAhorroService.ObtenerCuentaActivaPorNumeroAsync(cuenta);
            if (infoCuenta == null) return RedirectToAction(nameof(Retiro));

            var model = new ConfirmacionRetiroViewModel
            {
                NumeroCuentaOrigen = cuenta,
                Monto = monto,
                NombreTitular = infoCuenta.NombreTitular
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarRetiro(ConfirmacionRetiroViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("Iniciando proceso de retiro. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto: {Monto}", userId, model.NumeroCuentaOrigen, model.Monto);
            
            await _cuentaAhorroService.ActualizarBalanceAsync(model.NumeroCuentaOrigen, -model.Monto);

            var transaccion = _mapper.Map<TransaccionDto>(model);
            transaccion.UsuarioResponsable = userId;
            transaccion.Fecha = DateTime.Now;

            await _transaccionService.RegistrarTransaccionAsync(transaccion);

            _logger.LogInformation("Retiro procesado exitosamente en base de datos. Cajero: {UserId}, Cuenta Origen: {Cuenta}", userId, model.NumeroCuentaOrigen);

            try
            {
                var ultimosCuatro = model.NumeroCuentaOrigen.Length >= 4 
                    ? model.NumeroCuentaOrigen.Substring(model.NumeroCuentaOrigen.Length - 4) 
                    : model.NumeroCuentaOrigen;
                    
                var asunto = $"Retiro realizado desde su cuenta {ultimosCuatro}";
                var cuerpo = $@"Hola {model.NombreTitular},
Se ha realizado un retiro desde su cuenta terminada en {ultimosCuatro}.
Monto retirado: RD${model.Monto}
Fecha y hora: {DateTime.Now}
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

                await _emailService.EnviarCorreoAsync("cliente@example.com", asunto, cuerpo);
                _logger.LogInformation("Correo de notificación de retiro enviado. Cuenta: {Cuenta}", model.NumeroCuentaOrigen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "El retiro se procesó, pero ocurrió un error al enviar el correo al cliente. Cuenta: {Cuenta}", model.NumeroCuentaOrigen);
                TempData["InfoMessage"] = "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
                return RedirectToAction("Index", "Home");
            }

            TempData["SuccessMessage"] = "Retiro realizado correctamente.";
            return RedirectToAction("Index", "Home"); 
        }
    }
}
