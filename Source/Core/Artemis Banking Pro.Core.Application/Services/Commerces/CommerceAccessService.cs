using Artemis_Banking_Pro.Core.Application.Contracts.Commerces;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Commerces
{
    public sealed class CommerceAccessService : ICommerceAccessService
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CommerceAccessService> _logger;

        public CommerceAccessService(
            ICommerceRepository commerceRepository,
            ICurrentUserService currentUserService,
            ILogger<CommerceAccessService> logger)
        {
            _commerceRepository = commerceRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ValidationResult<Commerce>> ResolveCommerceAsync(int commerceIdFromRoute)
        {
            //El usuario con rol Comercio solo opera sobre el comercio asociado a su token: el
            //valor de la URL se ignora por completo.
            var commerce = _currentUserService.IsInRole(nameof(Roles.Comercio))
                ? await GetCommerceByAuthenticatedUserAsync()
                : await _commerceRepository.GetByIdAsync(commerceIdFromRoute);

            if (commerce is null)
                return ValidationResult<Commerce>.Failure(CommerceError.NonExistsCommerce);

            if (!commerce.IsActive)
                return ValidationResult<Commerce>.Failure(CommerceError.CommerceIsNotActive);

            return ValidationResult<Commerce>.Success(commerce);
        }

        private async Task<Commerce?> GetCommerceByAuthenticatedUserAsync()
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var commerce = await _commerceRepository.GetFirstAsync(
                entity => entity.AssociatedUserId == userId);

            if (commerce is null)
            {
                _logger.LogWarning(
                    "El usuario autenticado con rol Comercio no tiene un comercio asociado.");
            }

            return commerce;
        }
    }
}
