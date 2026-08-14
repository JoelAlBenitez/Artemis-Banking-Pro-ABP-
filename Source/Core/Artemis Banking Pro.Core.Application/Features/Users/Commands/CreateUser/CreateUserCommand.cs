using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateUser
{
    /// <summary>
    /// Datos de un usuario nuevo con rol Administrador, Cajero o Cliente.
    /// </summary>
    public class CreateUserCommand : IRequest<UserCreatedDto>
    {
        /// <example>María</example>
        [SwaggerParameter(Description = "Nombre del usuario")]
        public required string FirstName { get; set; }

        /// <example>Gómez</example>
        [SwaggerParameter(Description = "Apellido del usuario")]
        public required string LastName { get; set; }

        /// <example>00187654321</example>
        [SwaggerParameter(Description = "Cédula del usuario")]
        public required string Identification { get; set; }

        /// <example>cliente01@artemis.com</example>
        [SwaggerParameter(Description = "Correo electrónico del usuario")]
        public required string Email { get; set; }

        /// <example>cliente01</example>
        [SwaggerParameter(Description = "Nombre de usuario para iniciar sesión")]
        public required string UserName { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Contraseña inicial del usuario")]
        public required string Password { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Confirmación de la contraseña inicial")]
        public required string ConfirmPassword { get; set; }

        /// <example>Cliente</example>
        [SwaggerParameter(Description = "Rol del usuario: Administrador, Cajero o Cliente")]
        public required string Role { get; set; }

        /// <example>5000.00</example>
        [SwaggerParameter(Description = "Monto inicial de la cuenta principal. Solo aplica al rol Cliente")]
        public decimal? InitialAmount { get; set; }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserCreatedDto>
    {
        private readonly IAccountRegistrationService _accountRegistrationService;

        public CreateUserCommandHandler(IAccountRegistrationService accountRegistrationService)
        {
            _accountRegistrationService = accountRegistrationService;
        }

        public async Task<UserCreatedDto> Handle(
            CreateUserCommand command, CancellationToken cancellationToken)
        {
            var response = await _accountRegistrationService.RegisterUserAsync(new RegisterRequest
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                IDCARD = command.Identification,
                Email = command.Email,
                UserName = command.UserName,
                Password = command.Password,
                ConfirmPassword = command.ConfirmPassword,
                Role = command.Role,
                InitialAmount = command.InitialAmount,
                //El correo de la API lleva el token, no un enlace: por eso no viaja el origen
                Origin = null
            });

            if (response.HasError)
            {
                throw response.Conflict
                    ? new ConflictException(response.Error!)
                    : (Exception)new BusinessRuleException(response.Error!);
            }

            return new UserCreatedDto
            {
                Id = response.UserId!,
                UserName = command.UserName,
                Email = command.Email,
                Role = command.Role,
                //Todo usuario creado desde la API queda inactivo hasta confirmar su cuenta
                IsActive = false
            };
        }
    }
}
