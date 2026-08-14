using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Services.Commerces;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.HermesPay
{
    //Regla que distingue a Hermes Pay del resto de la API: el comercio efectivo depende del rol
    //autenticado, no solo de la URL.
    public sealed class CommerceAccessServiceTests
    {
        private const int CommerceFromRoute = 5;
        private const int CommerceOfTheUser = 9;

        private readonly Mock<ICommerceRepository> _commerceRepository = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        [Fact]
        public async Task ResolveCommerce_AsAdministrator_ShouldUseTheCommerceFromTheRoute()
        {
            GivenRole(Roles.Administrador);
            _commerceRepository.Setup(repository => repository.GetByIdAsync(CommerceFromRoute))
                .ReturnsAsync(BuildCommerce(CommerceFromRoute));

            var result = await BuildService().ResolveCommerceAsync(CommerceFromRoute);

            result.IsValid.Should().BeTrue();
            result.Value!.Id.Should().Be(CommerceFromRoute);
        }

        //Un usuario Comercio solo opera sobre el suyo: el commerceId de la URL se ignora.
        [Fact]
        public async Task ResolveCommerce_AsCommerce_ShouldIgnoreTheRouteAndUseTheTokenCommerce()
        {
            GivenRole(Roles.Comercio);
            _currentUserService.SetupGet(service => service.UserId).Returns("usuario-comercio");

            _commerceRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync(BuildCommerce(CommerceOfTheUser));

            var result = await BuildService().ResolveCommerceAsync(CommerceFromRoute);

            result.Value!.Id.Should().Be(CommerceOfTheUser);
            _commerceRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ResolveCommerce_AsCommerceWithoutAssociatedCommerce_ShouldFail()
        {
            GivenRole(Roles.Comercio);
            _currentUserService.SetupGet(service => service.UserId).Returns("usuario-sin-comercio");

            _commerceRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync((Commerce?)null);

            var result = await BuildService().ResolveCommerceAsync(CommerceFromRoute);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CommerceError.NonExistsCommerce);
        }

        //Un comercio inactivo no consulta ni procesa pagos.
        [Fact]
        public async Task ResolveCommerce_WithInactiveCommerce_ShouldRejectTheOperation()
        {
            GivenRole(Roles.Administrador);

            var commerce = BuildCommerce(CommerceFromRoute);
            commerce.Status = CommerceStatus.Inactivo;
            _commerceRepository.Setup(repository => repository.GetByIdAsync(CommerceFromRoute))
                .ReturnsAsync(commerce);

            var result = await BuildService().ResolveCommerceAsync(CommerceFromRoute);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CommerceError.CommerceIsNotActive);
        }

        [Fact]
        public async Task ResolveCommerce_WithUnknownCommerce_ShouldFail()
        {
            GivenRole(Roles.Administrador);
            _commerceRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Commerce)null!);

            var result = await BuildService().ResolveCommerceAsync(999);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CommerceError.NonExistsCommerce);
        }

        private CommerceAccessService BuildService()
            => new(_commerceRepository.Object,
                   _currentUserService.Object,
                   NullLogger<CommerceAccessService>.Instance);

        private void GivenRole(Roles role)
            => _currentUserService
                .Setup(service => service.IsInRole(nameof(Roles.Comercio)))
                .Returns(role == Roles.Comercio);

        private static Commerce BuildCommerce(int id)
            => new()
            {
                Id = id,
                Name = "Tienda Demo",
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999",
                Status = CommerceStatus.Activo,
                AssociatedUserId = "usuario-comercio",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };
    }
}
