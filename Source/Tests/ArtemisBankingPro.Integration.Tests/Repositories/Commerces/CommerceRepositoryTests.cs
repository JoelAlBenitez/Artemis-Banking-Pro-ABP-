using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Commerces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Commerces
{
    //Contrastan el repositorio contra el DbContext real. El proveedor en memoria no hace
    //cumplir los índices únicos, así que la unicidad de RNC, correo y usuario asociado queda
    //fuera de estas pruebas: solo es verificable contra SQL Server con migraciones. La
    //construcción del modelo, incluidos esos índices, sí la cubre DbContextModelTests.
    public sealed class CommerceRepositoryTests : IDisposable
    {
        private readonly DbContextArtemisBanking _context;
        private readonly CommerceRepository _repository;

        public CommerceRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"commerces-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new CommerceRepository(_context);
        }

        #region orden y filtros del listado

        [Fact]
        public async Task GetPagedCommercesAsync_ShouldOrderFromTheNewestToTheOldest()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenCommerces(
                BuildCommerce("101000001", createdAt: today.AddDays(-30)),
                BuildCommerce("101000002", createdAt: today.AddDays(-1)),
                BuildCommerce("101000003", createdAt: today.AddDays(-10)));

            var result = await _repository.GetPagedCommercesAsync(1, 20, null);

            result.Items.Select(commerce => commerce.Rnc)
                .Should().Equal("101000002", "101000003", "101000001");
        }

        [Fact]
        public async Task GetPagedCommercesAsync_WithActiveFilter_ShouldExcludeTheInactiveOnes()
        {
            await GivenCommerces(
                BuildCommerce("101000001", status: CommerceStatus.Activo),
                BuildCommerce("101000002", status: CommerceStatus.Inactivo),
                BuildCommerce("101000003", status: CommerceStatus.Activo));

            var result = await _repository.GetPagedCommercesAsync(1, 20, CommerceStatus.Activo);

            result.TotalRecords.Should().Be(2);
            result.Items.Should().OnlyContain(commerce => commerce.Status == CommerceStatus.Activo);
        }

        [Fact]
        public async Task GetPagedCommercesAsync_WithoutStatusFilter_ShouldReturnBoth()
        {
            await GivenCommerces(
                BuildCommerce("101000001", status: CommerceStatus.Activo),
                BuildCommerce("101000002", status: CommerceStatus.Inactivo));

            var result = await _repository.GetPagedCommercesAsync(1, 20, null);

            result.TotalRecords.Should().Be(2);
        }

        #endregion

        #region paginación

        //Ningún listado administrativo devuelve más de 20 registros por página.
        [Fact]
        public async Task GetPagedCommercesAsync_WithPageSizeAboveTheMaximum_ShouldClampItToTwenty()
        {
            var commerces = Enumerable.Range(1, 25)
                .Select(index => BuildCommerce($"1010000{index:D2}"))
                .ToArray();

            await GivenCommerces(commerces);

            var result = await _repository.GetPagedCommercesAsync(1, 50, null);

            result.Items.Should().HaveCount(20);
            result.PageSize.Should().Be(20);
            result.TotalRecords.Should().Be(25);
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedCommercesAsync_OnTheSecondPage_ShouldReturnTheRemainingRecords()
        {
            var commerces = Enumerable.Range(1, 25)
                .Select(index => BuildCommerce($"1010000{index:D2}"))
                .ToArray();

            await GivenCommerces(commerces);

            var result = await _repository.GetPagedCommercesAsync(2, 20, null);

            result.Items.Should().HaveCount(5);
            result.Page.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedCommercesAsync_WithAnInvalidPage_ShouldFallBackToTheFirstOne()
        {
            await GivenCommerces(BuildCommerce("101000001"));

            var result = await _repository.GetPagedCommercesAsync(0, 20, null);

            result.Page.Should().Be(1);
            result.Items.Should().HaveCount(1);
        }

        #endregion

        #region persistencia

        [Fact]
        public async Task AddAsync_ShouldPersistTheCommerceWithItsAuditData()
        {
            var commerce = BuildCommerce("101999999");

            await _repository.AddAsync(commerce);
            await _repository.SaveChangesAsync();

            var stored = await _context.Commerces.SingleAsync();
            stored.Name.Should().Be("Tienda Demo");
            stored.Status.Should().Be(CommerceStatus.Activo);
            stored.CreateByUserId.Should().Be("admin-1");
        }

        //La asociación con el usuario de Identity se cierra después de crear el comercio.
        [Fact]
        public async Task UpdateAsync_ShouldPersistTheAssociatedUser()
        {
            var commerce = BuildCommerce("101999999");
            await GivenCommerces(commerce);

            commerce.AssociatedUserId = "usuario-comercio";
            await _repository.UpdateAsync(commerce);
            await _repository.SaveChangesAsync();

            var stored = await _context.Commerces.SingleAsync();
            stored.AssociatedUserId.Should().Be("usuario-comercio");
            stored.HasAssociatedUser.Should().BeTrue();
        }

        //La baja de un comercio es un cambio de estado: nunca se elimina físicamente.
        [Fact]
        public async Task UpdateAsync_WhenDeactivating_ShouldKeepTheCommerceInTheDatabase()
        {
            var commerce = BuildCommerce("101999999");
            await GivenCommerces(commerce);

            commerce.Status = CommerceStatus.Inactivo;
            await _repository.UpdateAsync(commerce);
            await _repository.SaveChangesAsync();

            _context.Commerces.Should().HaveCount(1);
            (await _context.Commerces.SingleAsync()).Status.Should().Be(CommerceStatus.Inactivo);
        }

        [Fact]
        public async Task ExistElementByConsult_ShouldDetectARepeatedRnc()
        {
            await GivenCommerces(BuildCommerce("101999999"));

            (await _repository.ExistElementByConsult(commerce => commerce.Rnc == "101999999"))
                .Should().BeTrue();

            (await _repository.ExistElementByConsult(commerce => commerce.Rnc == "101000000"))
                .Should().BeFalse();
        }

        [Fact]
        public async Task GetFirstAsync_ShouldFindTheCommerceByItsAssociatedUser()
        {
            await GivenCommerces(
                BuildCommerce("101000001", associatedUserId: "otro-usuario"),
                BuildCommerce("101000002", associatedUserId: "usuario-comercio"));

            var commerce = await _repository.GetFirstAsync(
                entity => entity.AssociatedUserId == "usuario-comercio");

            commerce.Should().NotBeNull();
            commerce!.Rnc.Should().Be("101000002");
        }

        #endregion

        #region builders

        private async Task GivenCommerces(params Commerce[] commerces)
        {
            await _context.Commerces.AddRangeAsync(commerces);
            await _context.SaveChangesAsync();
        }

        private static Commerce BuildCommerce(
            string rnc,
            CommerceStatus status = CommerceStatus.Activo,
            DateTimeOffset? createdAt = null,
            string? associatedUserId = null)
            => new()
            {
                Name = "Tienda Demo",
                Description = "Comercio de prueba para pagos Hermes Pay",
                Email = $"contacto{rnc}@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = rnc,
                Status = status,
                AssociatedUserId = associatedUserId,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        #endregion

        public void Dispose() => _context.Dispose();
    }
}
