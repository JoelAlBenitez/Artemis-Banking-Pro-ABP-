using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
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

            var resultMessage = await _accountRegistrationService.ConfirmAccountAsync(request.UserId, request.Token);

            if (resultMessage.Contains("no es válido") || resultMessage.Contains("ya fue utilizado"))
                return BadRequest(new { error = resultMessage });

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
        public required string UserId { get; set; }
        public required string Token { get; set; }
    }
}

