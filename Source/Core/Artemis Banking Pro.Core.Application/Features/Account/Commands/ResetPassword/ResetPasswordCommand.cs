using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ResetPassword
{
    /// <summary>
    /// Nueva contraseña y token de restablecimiento recibido por correo.
    /// </summary>
    public class ResetPasswordCommand : IRequest
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Identificador del usuario al que se le cambiará la contraseña")]
        public required string UserId { get; set; }

        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9</example>
        [SwaggerParameter(Description = "Token de restablecimiento enviado al correo del usuario")]
        public required string Token { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Nueva contraseña del usuario")]
        public required string Password { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Confirmación de la nueva contraseña")]
        public required string ConfirmPassword { get; set; }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public ResetPasswordCommandHandler(IPasswordRecoveryService passwordRecoveryService)
        {
            _passwordRecoveryService = passwordRecoveryService;
        }

        public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            //Cambia la contraseña, marca el token como utilizado y reactiva la cuenta que el
            //endpoint de get-reset-token había inactivado.
            var response = await _passwordRecoveryService.ResetPasswordApiAsync(new ResetPasswordApiRequest
            {
                UserId = command.UserId,
                Token = command.Token,
                Password = command.Password,
                ConfirmPassword = command.ConfirmPassword
            });

            if (response.HasError)
                throw new BusinessRuleException(response.Error!);
        }
    }
}
