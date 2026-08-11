using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ConfirmAccount
{
    /// <summary>
    /// Token de confirmación con el que se activa una cuenta creada desde la Web API.
    /// </summary>
    public class ConfirmAccountCommand : IRequest
    {
        /// <example>2</example>
        [SwaggerParameter(Description = "Identificador del usuario que acompaña al token en el correo de activación")]
        public required string UserId { get; set; }

        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9</example>
        [SwaggerParameter(Description = "Token de confirmación enviado al correo del usuario")]
        public required string Token { get; set; }
    }

    public class ConfirmAccountCommandHandler : IRequestHandler<ConfirmAccountCommand>
    {
        private readonly IAccountRegistrationService _accountRegistrationService;

        public ConfirmAccountCommandHandler(IAccountRegistrationService accountRegistrationService)
        {
            _accountRegistrationService = accountRegistrationService;
        }

        public async Task Handle(ConfirmAccountCommand command, CancellationToken cancellationToken)
        {
            var response = await _accountRegistrationService.ConfirmAccountAsync(command.UserId, command.Token);

            if (response.HasError)
                throw new BusinessRuleException(response.Message);
        }
    }
}
