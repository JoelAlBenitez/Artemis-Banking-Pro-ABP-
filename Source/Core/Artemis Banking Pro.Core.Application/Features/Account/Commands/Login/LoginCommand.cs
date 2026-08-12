using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.Login
{
    /// <summary>
    /// Credenciales de un usuario de la Web API.
    /// </summary>
    public class LoginCommand : IRequest<JwtTokenDto>
    {
        /// <example>admin</example>
        [SwaggerParameter(Description = "Nombre de usuario registrado en el sistema")]
        public required string UserName { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Contraseña asociada al usuario")]
        public required string Password { get; set; }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, JwtTokenDto>
    {
        private readonly IAuthWebApiService _authService;

        public LoginCommandHandler(IAuthWebApiService authService)
        {
            _authService = authService;
        }

        public async Task<JwtTokenDto> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(new AuthenticationRequest
            {
                UserName = command.UserName,
                Password = command.Password
            });

            //Un rol ajeno a la API es un rechazo de permisos; credenciales inválidas y cuenta
            //inactiva son rechazos de autenticación.
            if (response.HasError && response.Forbidden)
                throw new ForbiddenException(response.Error!);

            if (response.HasError)
                throw new UnauthorizedException(response.Error!);

            return new JwtTokenDto { Jwt = response.Token };
        }
    }
}
