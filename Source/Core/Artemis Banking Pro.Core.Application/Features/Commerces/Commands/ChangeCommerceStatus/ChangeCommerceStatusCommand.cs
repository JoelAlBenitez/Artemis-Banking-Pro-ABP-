using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using MediatR;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.ChangeCommerceStatus
{
    /// <summary>
    /// Nuevo estado de un comercio.
    /// </summary>
    public class ChangeCommerceStatusCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del comercio al que se le cambiará el estado")]
        public int Id { get; set; }

        /// <example>true</example>
        [SwaggerParameter(Description = "Nuevo estado del comercio. true para activo, false para inactivo")]
        public required bool Status { get; set; }
    }

    public class ChangeCommerceStatusCommandHandler : IRequestHandler<ChangeCommerceStatusCommand>
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ChangeCommerceStatusCommandHandler> _logger;

        public ChangeCommerceStatusCommandHandler(
            ICommerceRepository commerceRepository,
            IUserManagementService userManagementService,
            ICurrentUserService currentUserService,
            ILogger<ChangeCommerceStatusCommandHandler> logger)
        {
            _commerceRepository = commerceRepository;
            _userManagementService = userManagementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task Handle(ChangeCommerceStatusCommand command, CancellationToken cancellationToken)
        {
            var commerce = await _commerceRepository.GetByIdAsync(command.Id)
                ?? throw new NotFoundException(CommerceError.NonExistsCommerce);

            commerce.Status = command.Status ? CommerceStatus.Activo : CommerceStatus.Inactivo;
            commerce.LastModifiedByIdUser = _currentUserService.UserId;
            commerce.ModifiedAt = DateTimeOffset.UtcNow;

            await _commerceRepository.UpdateAsync(commerce);
            await _commerceRepository.SaveChangesAsync();

            //Reactivar un comercio no reactiva a sus usuarios: deben completar el proceso de
            //restablecimiento de contraseña. La cascada solo ocurre al desactivar.
            if (command.Status || !commerce.HasAssociatedUser)
                return;

            await InactivateAssociatedUserAsync(commerce.Id, commerce.AssociatedUserId!);
        }

        //El comercio vive en Persistence y sus usuarios en Identity: al no compartir DbContext
        //no hay transacción que cubra ambos lados. El fallo del segundo paso se registra y no
        //revierte el cambio de estado del comercio.
        private async Task InactivateAssociatedUserAsync(int commerceId, string associatedUserId)
        {
            var result = await _userManagementService.SetUserStatusAsync(associatedUserId, false);

            if (result.HasError)
            {
                _logger.LogWarning(
                    "El comercio {CommerceId} quedó inactivo, pero no fue posible inactivar a su usuario asociado. Detalle: {Detalle}",
                    commerceId, result.Error);
            }
        }
    }
}
