using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    //Flujo de autenticación: ninguno de estos endpoints puede exigir un JWT previo, porque
    //quien todavía no ha activado su cuenta o perdió su contraseña no puede tener uno.
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly IAuthWebApiService _authService;
        private readonly IAccountRegistrationService _accountRegistrationService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public AccountController(
            IAuthWebApiService authService,
            IAccountRegistrationService accountRegistrationService,
            IPasswordRecoveryService passwordRecoveryService)
        {
            _authService = authService;
            _accountRegistrationService = accountRegistrationService;
            _passwordRecoveryService = passwordRecoveryService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);

            if (response.HasError)
            {
                if (response.Forbidden)
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = response.Error });

                return Unauthorized(new { error = response.Error });
            }

            return Ok(new { jwt = response.Token });
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmAccount([FromBody] ConfirmAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _accountRegistrationService.ConfirmAccountAsync(request.UserId, request.Token);

            if (response.HasError)
                return BadRequest(new { error = response.Message });

            return NoContent();
        }

        [HttpPost("get-reset-token")]
        public async Task<IActionResult> GetResetToken([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _passwordRecoveryService.ForgotPasswordApiAsync(request);

            if (response.HasError)
                return BadRequest(new { error = response.Error });

            return NoContent();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordApiRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _passwordRecoveryService.ResetPasswordApiAsync(request);

            if (response.HasError)
                return BadRequest(new { error = response.Error });

            return NoContent();
        }
    }

    public class ConfirmAccountRequest
    {
        //El token de Identity va cifrado y no permite recuperar al usuario: el identificador
        //viaja junto a él y se envía en el mismo correo de activación.
        [Required(AllowEmptyStrings = false)]
        public required string UserId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Token { get; set; }
    }
}
