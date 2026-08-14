using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.UpdateCommerce
{
    /// <summary>
    /// Datos actualizables de un comercio. El estado no se modifica desde aquí.
    /// </summary>
    public class UpdateCommerceCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del comercio que se desea actualizar")]
        public int Id { get; set; }

        /// <example>Tienda Demo Actualizada</example>
        [SwaggerParameter(Description = "Nombre comercial del comercio")]
        public required string Name { get; set; }

        /// <example>Comercio actualizado para pagos Hermes Pay</example>
        [SwaggerParameter(Description = "Descripción general del comercio")]
        public string? Description { get; set; }

        /// <example>contacto.actualizado@tiendademo.com</example>
        [SwaggerParameter(Description = "Correo electrónico de contacto del comercio")]
        public required string Email { get; set; }

        /// <example>8095555678</example>
        [SwaggerParameter(Description = "Número telefónico del comercio")]
        public required string PhoneNumber { get; set; }

        /// <example>101999999</example>
        [SwaggerParameter(Description = "Identificador fiscal o RNC del comercio")]
        public required string Rnc { get; set; }
    }

    public class UpdateCommerceCommandHandler : IRequestHandler<UpdateCommerceCommand>
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCommerceCommandHandler(
            ICommerceRepository commerceRepository,
            ICurrentUserService currentUserService)
        {
            _commerceRepository = commerceRepository;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateCommerceCommand command, CancellationToken cancellationToken)
        {
            var commerce = await _commerceRepository.GetByIdAsync(command.Id)
                ?? throw new NotFoundException(CommerceError.NonExistsCommerce);

            if (await _commerceRepository.ExistElementByConsult(
                    other => other.Rnc == command.Rnc && other.Id != command.Id))
                throw new ConflictException(CommerceError.RncAlreadyRegistered);

            if (await _commerceRepository.ExistElementByConsult(
                    other => other.Email == command.Email && other.Id != command.Id))
                throw new ConflictException(CommerceError.EmailAlreadyRegistered);

            commerce.Name = command.Name;
            commerce.Description = command.Description;
            commerce.Email = command.Email;
            commerce.PhoneNumber = command.PhoneNumber;
            commerce.Rnc = command.Rnc;
            commerce.LastModifiedByIdUser = _currentUserService.UserId;
            commerce.ModifiedAt = DateTimeOffset.UtcNow;

            await _commerceRepository.UpdateAsync(commerce);
            await _commerceRepository.SaveChangesAsync();
        }
    }
}
