using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ConfirmAccount;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.GetResetToken;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.Login;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ResetPassword;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    //Flujo de autenticación: ninguno de estos endpoints puede exigir un JWT previo, porque
    //quien todavía no ha activado su cuenta o perdió su contraseña no puede tener uno.
    [Route("account")]
    [AllowAnonymous]
    [SwaggerTag("Autenticación, activación de cuenta y restablecimiento de contraseña")]
    public class AccountController : BaseApiController
    {
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JwtTokenDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Iniciar sesión",
            Description = "Valida las credenciales de un usuario Administrador o Comercio y retorna su token JWT")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            return Ok(await Mediator.Send(command));
        }

        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Confirmar cuenta",
            Description = "Activa la cuenta de un usuario mediante el token de confirmación enviado por correo")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAccountCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPost("get-reset-token")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Obtener token de restablecimiento",
            Description = "Inactiva temporalmente la cuenta, genera el token de restablecimiento y lo envía al correo del usuario")]
        public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Restablecer contraseña",
            Description = "Cambia la contraseña usando el token de restablecimiento y deja la cuenta activa")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
