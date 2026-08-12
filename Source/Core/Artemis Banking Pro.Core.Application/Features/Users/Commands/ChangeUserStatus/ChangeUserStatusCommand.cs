using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.ChangeUserStatus
{
    /// <summary>
    /// Nuevo estado de un usuario.
    /// </summary>
    public class ChangeUserStatusCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del usuario al que se le cambiará el estado")]
        public string Id { get; set; } = string.Empty;

        /// <example>true</example>
        [SwaggerParameter(Description = "Nuevo estado del usuario. true para activo, false para inactivo")]
        public required bool Status { get; set; }
    }

    public class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
    {
        public ChangeUserStatusCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");
        }
    }

    public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand>
    {
        private readonly IUserManagementService _userManagementService;

        public ChangeUserStatusCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task Handle(ChangeUserStatusCommand command, CancellationToken cancellationToken)
        {
            //El servicio resuelve por sí solo quién es el administrador autenticado y rechaza
            //el intento de auto-modificación: el claim no viaja por parámetro.
            var response = await _userManagementService.SetUserStatusAsync(command.Id, command.Status);

            if (!response.HasError)
                return;

            if (response.NotFound)
                throw new NotFoundException(response.Error!);

            //El documento responde el intento de auto-modificación como acceso denegado
            throw new ForbiddenException(response.Error!);
        }
    }
}
