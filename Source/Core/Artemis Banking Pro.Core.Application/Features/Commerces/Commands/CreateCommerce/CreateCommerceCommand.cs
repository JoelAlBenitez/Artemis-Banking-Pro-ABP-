using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using AutoMapper;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.CreateCommerce
{
    /// <summary>
    /// Datos de un comercio nuevo. El usuario con rol Comercio se crea después, desde su
    /// propio endpoint del módulo de gestión de usuarios.
    /// </summary>
    public class CreateCommerceCommand : IRequest<CommerceListItemDto>
    {
        /// <example>Tienda Demo</example>
        [SwaggerParameter(Description = "Nombre comercial del comercio")]
        public required string Name { get; set; }

        /// <example>Comercio de prueba para pagos Hermes Pay</example>
        [SwaggerParameter(Description = "Descripción general del comercio")]
        public string? Description { get; set; }

        /// <example>contacto@tiendademo.com</example>
        [SwaggerParameter(Description = "Correo electrónico de contacto del comercio")]
        public required string Email { get; set; }

        /// <example>8095551234</example>
        [SwaggerParameter(Description = "Número telefónico del comercio")]
        public required string PhoneNumber { get; set; }

        /// <example>101999999</example>
        [SwaggerParameter(Description = "Identificador fiscal o RNC del comercio")]
        public required string Rnc { get; set; }
    }

    public class CreateCommerceCommandHandler
        : IRequestHandler<CreateCommerceCommand, CommerceListItemDto>
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public CreateCommerceCommandHandler(
            ICommerceRepository commerceRepository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _commerceRepository = commerceRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<CommerceListItemDto> Handle(
            CreateCommerceCommand command, CancellationToken cancellationToken)
        {
            var administratorId = _currentUserService.UserId
                ?? throw new BusinessRuleException(CommerceError.AdminUserRequired.Description);

            if (await _commerceRepository.ExistElementByConsult(commerce => commerce.Rnc == command.Rnc))
                throw new ConflictException(CommerceError.RncAlreadyRegistered);

            if (await _commerceRepository.ExistElementByConsult(commerce => commerce.Email == command.Email))
                throw new ConflictException(CommerceError.EmailAlreadyRegistered);

            var commerce = new Commerce
            {
                Name = command.Name,
                Description = command.Description,
                Email = command.Email,
                PhoneNumber = command.PhoneNumber,
                Rnc = command.Rnc,
                Status = CommerceStatus.Activo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = administratorId
            };

            await _commerceRepository.AddAsync(commerce);
            await _commerceRepository.SaveChangesAsync();

            return _mapper.Map<CommerceListItemDto>(commerce);
        }
    }
}
