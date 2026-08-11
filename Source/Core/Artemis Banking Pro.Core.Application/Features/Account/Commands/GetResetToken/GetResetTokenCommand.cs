using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.GetResetToken
{
    /// <summary>
    /// Solicitud del token con el que un usuario de la Web API restablece su contraseña.
    /// </summary>
    public class GetResetTokenCommand : IRequest
    {
        /// <example>admin</example>
        [SwaggerParameter(Description = "Nombre de usuario de la cuenta que solicita el restablecimiento")]
        public required string UserName { get; set; }
    }

    public class GetResetTokenCommandHandler : IRequestHandler<GetResetTokenCommand>
    {
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public GetResetTokenCommandHandler(IPasswordRecoveryService passwordRecoveryService)
        {
            _passwordRecoveryService = passwordRecoveryService;
        }

        public async Task Handle(GetResetTokenCommand command, CancellationToken cancellationToken)
        {
            //La variante de API inactiva temporalmente la cuenta y envía el token en el cuerpo
            //del correo, no como enlace.
            var response = await _passwordRecoveryService.ForgotPasswordApiAsync(
                new ForgotPasswordRequest { UserName = command.UserName });

            if (response.HasError)
                throw new BusinessRuleException(response.Error!);
        }
    }
}
