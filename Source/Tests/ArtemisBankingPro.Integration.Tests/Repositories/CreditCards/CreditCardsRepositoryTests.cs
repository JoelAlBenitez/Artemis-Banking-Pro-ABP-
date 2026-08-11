using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.CreditCards
{
    //Contrastan los repositorios de tarjetas contra el DbContext real. El proveedor en memoria
    //no hace cumplir el índice único de CardNumber, así que la unicidad efectiva del número solo
    //es verificable contra SQL Server con migraciones; aquí se cubre la consulta que la sostiene
    //(ExistElementByConsult) y la construcción del modelo la cubre DbContextModelTests.
    public sealed class CreditCardsRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-1";
        private const string OtherCustomerId = "customer-2";

        private readonly DbContextArtemisBanking _context;
        private readonly CreditCardsRepository _repository;
        private readonly CardConsumptionRepository _consumptionRepository;

        public CreditCardsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"credit-cards-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new CreditCardsRepository(_context);
            _consumptionRepository = new CardConsumptionRepository(_context);
        }

        #region orden y filtros del listado
        //Regla propia del módulo: buscando sin estado, las activas van primero y las canceladas
        //después; dentro de cada grupo, de la más reciente a la más antigua.
        [Fact]
        public async Task GetPagedCreditCardsAsync_WithoutStatusFilter_ShouldShowActiveCardsFirst()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Cancelada, createdAt: today.AddDays(-1)),
                BuildCard("1111111111110002", CreditCardStatus.Activa, createdAt: today.AddDays(-10)),
                BuildCard("1111111111110003", CreditCardStatus.Cancelada, createdAt: today.AddDays(-30)),
                BuildCard("1111111111110004", CreditCardStatus.Activa, createdAt: today.AddDays(-2)));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, null);

            result.Items.Select(card => card.CardNumber)
                .Should().Equal("1111111111110004", "1111111111110002", "1111111111110001", "1111111111110003");
        }

        //Con filtro de estado el criterio es uno solo: de la más reciente a la más antigua.
        [Fact]
        public async Task GetPagedCreditCardsAsync_WithStatusFilter_ShouldOrderFromNewestToOldest()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Activa, createdAt: today.AddDays(-5)),
                BuildCard("1111111111110002", CreditCardStatus.Activa, createdAt: today),
                BuildCard("1111111111110003", CreditCardStatus.Cancelada, createdAt: today),
                BuildCard("1111111111110004", CreditCardStatus.Activa, createdAt: today.AddDays(-1)));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, CreditCardStatus.Activa, null);

            result.TotalRecords.Should().Be(3);
            result.Items.Select(card => card.CardNumber)
                .Should().Equal("1111111111110002", "1111111111110004", "1111111111110001");
        }

        [Theory]
        [InlineData(CreditCardStatus.Activa, "1111111111110001")]
        [InlineData(CreditCardStatus.Cancelada, "1111111111110002")]
        public async Task GetPagedCreditCardsAsync_WithStatusFilter_ShouldOnlyReturnThatStatus(
            CreditCardStatus status, string expectedCardNumber)
        {
            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Activa),
                BuildCard("1111111111110002", CreditCardStatus.Cancelada));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, status, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Single().CardNumber.Should().Be(expectedCardNumber);
        }

        //Un cliente puede tener varias tarjetas: la búsqueda por cédula las trae todas.
        [Fact]
        public async Task GetPagedCreditCardsAsync_WithCustomerFilter_ShouldReturnEveryCardOfThatCustomer()
        {
            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Activa),
                BuildCard("1111111111110002", CreditCardStatus.Activa, OtherCustomerId),
                BuildCard("1111111111110003", CreditCardStatus.Cancelada, OtherCustomerId));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, OtherCustomerId);

            result.TotalRecords.Should().Be(2);
            result.Items.Should().OnlyContain(card => card.CustomerId == OtherCustomerId);
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_ShouldCombineStatusAndCustomerFilters()
        {
            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Activa),
                BuildCard("1111111111110002", CreditCardStatus.Cancelada),
                BuildCard("1111111111110003", CreditCardStatus.Activa, OtherCustomerId));

            var result = await _repository.GetPagedCreditCardsAsync(
                1, 20, CreditCardStatus.Activa, CustomerId);

            result.TotalRecords.Should().Be(1);
            result.Items.Single().CardNumber.Should().Be("1111111111110001");
        }

        //Un cliente sin tarjetas devuelve el listado vacío, no una excepción: el mensaje
        //«Este cliente no tiene tarjetas de crédito registradas» lo decide el servicio.
        [Fact]
        public async Task GetPagedCreditCardsAsync_WithoutMatches_ShouldReturnAnEmptyPage()
        {
            await GivenCreditCards(BuildCard("1111111111110001", CreditCardStatus.Activa));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, "customer-sin-tarjetas");

            result.Items.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }
        #endregion

        #region paginación
        [Fact]
        public async Task GetPagedCreditCardsAsync_ShouldNeverExceedTwentyRecordsPerPage()
        {
            await GivenCreditCards(BuildManyActiveCards(25));

            var firstPage = await _repository.GetPagedCreditCardsAsync(1, 50, null, null);

            firstPage.Items.Should().HaveCount(20);
            firstPage.PageSize.Should().Be(20);
            firstPage.TotalRecords.Should().Be(25);
            firstPage.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_SecondPage_ShouldContinueWhereTheFirstEnded()
        {
            await GivenCreditCards(BuildManyActiveCards(25));

            var firstPage = await _repository.GetPagedCreditCardsAsync(1, 20, null, null);
            var secondPage = await _repository.GetPagedCreditCardsAsync(2, 20, null, null);

            secondPage.Items.Should().HaveCount(5);
            secondPage.Page.Should().Be(2);

            //Ninguna tarjeta se repite ni se pierde entre páginas
            var everything = firstPage.Items.Concat(secondPage.Items).Select(card => card.CardNumber).ToList();
            everything.Should().OnlyHaveUniqueItems().And.HaveCount(25);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task GetPagedCreditCardsAsync_WithAnInvalidPage_ShouldFallBackToTheFirstOne(int page)
        {
            await GivenCreditCards(BuildManyActiveCards(3));

            var result = await _repository.GetPagedCreditCardsAsync(page, 20, null, null);

            result.Page.Should().Be(1);
            result.Items.Should().HaveCount(3);
        }

        //El listado es de solo lectura: no arrastra las tarjetas al ChangeTracker.
        [Fact]
        public async Task GetPagedCreditCardsAsync_ShouldNotTrackTheReturnedCards()
        {
            await GivenCreditCards(BuildCard("1111111111110001", CreditCardStatus.Activa));

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, null);

            result.Items.Should().NotBeEmpty();
            _context.ChangeTracker.Entries<CreditCard>().Should().BeEmpty();
        }
        #endregion

        #region unicidad del número y cancelación
        //Consulta que sostiene la unicidad del número de 16 dígitos: es la que repite el
        //generador hasta encontrar un candidato libre.
        [Fact]
        public async Task ExistElementByConsult_ShouldDetectAnAlreadyRegisteredCardNumber()
        {
            await GivenCreditCards(BuildCard("1111111111110001", CreditCardStatus.Activa));

            (await _repository.ExistElementByConsult(card => card.CardNumber == "1111111111110001"))
                .Should().BeTrue();

            (await _repository.ExistElementByConsult(card => card.CardNumber == "9999999999999999"))
                .Should().BeFalse();
        }

        //El número se persiste como texto: los ceros iniciales sobreviven al viaje.
        [Fact]
        public async Task AddAsync_ShouldPreserveLeadingZerosOfTheCardNumber()
        {
            var card = BuildCard("0000111122223333", CreditCardStatus.Activa);

            await _repository.AddAsync(card);
            await _repository.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var persisted = await _context.CreditCards.SingleAsync();

            persisted.CardNumber.Should().Be("0000111122223333");
            persisted.CardNumber.Should().HaveLength(16);
        }

        //Cancelar es un cambio de estado: la tarjeta y su historial siguen en la base.
        [Fact]
        public async Task UpdateAsync_ShouldCancelWithoutDeletingTheCardOrItsConsumptions()
        {
            var card = BuildCard("1111111111110001", CreditCardStatus.Activa);
            await GivenCreditCards(card);
            await GivenConsumptions(
                BuildConsumption(card.Id, 500m, ConsumptionStatus.Aprobado),
                BuildConsumption(card.Id, 800m, ConsumptionStatus.Rechazado, RejectionReason.CreditoInsuficiente));

            var toCancel = await _repository.GetByIdAsync(card.Id);
            toCancel.Status = CreditCardStatus.Cancelada;
            toCancel.ModifiedAt = DateTimeOffset.UtcNow;
            toCancel.LastModifiedByIdUser = "admin-1";

            await _repository.UpdateAsync(toCancel);
            await _repository.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var cancelled = await _context.CreditCards.SingleAsync();

            cancelled.Status.Should().Be(CreditCardStatus.Cancelada);
            cancelled.LastModifiedByIdUser.Should().Be("admin-1");

            //No hay borrado físico: la tarjeta cancelada conserva sus dos consumos
            (await _context.CreditCards.CountAsync()).Should().Be(1);
            (await _context.CardConsumptions.CountAsync(c => c.CreditCardId == card.Id)).Should().Be(2);
        }

        //Indicador del Home y cálculo de deuda: solo suman las tarjetas activas.
        [Fact]
        public async Task SumAsync_ShouldOnlyAddUpTheDebtOfActiveCards()
        {
            await GivenCreditCards(
                BuildCard("1111111111110001", CreditCardStatus.Activa, owedAmount: 1500m),
                BuildCard("1111111111110002", CreditCardStatus.Activa, owedAmount: 500m),
                BuildCard("1111111111110003", CreditCardStatus.Cancelada, owedAmount: 9000m));

            var debt = await _repository.SumAsync(
                card => card.Status == CreditCardStatus.Activa,
                card => card.OwedAmount);

            debt.Should().Be(2000m);
        }
        #endregion

        #region historial de consumos
        //Ver detalles: del consumo más reciente al más antiguo, aprobados y rechazados juntos.
        [Fact]
        public async Task GetAllAsync_ShouldReturnTheConsumptionsOfTheCardFromNewestToOldest()
        {
            var card = BuildCard("1111111111110001", CreditCardStatus.Activa);
            var otherCard = BuildCard("1111111111110002", CreditCardStatus.Activa);
            await GivenCreditCards(card, otherCard);

            var today = DateTimeOffset.UtcNow;

            await GivenConsumptions(
                BuildConsumption(card.Id, 100m, ConsumptionStatus.Aprobado, createdAt: today.AddDays(-3)),
                BuildConsumption(card.Id, 200m, ConsumptionStatus.Rechazado,
                    RejectionReason.CreditoInsuficiente, createdAt: today.AddDays(-1)),
                BuildConsumption(card.Id, 300m, ConsumptionStatus.Aprobado, createdAt: today.AddDays(-2)),
                BuildConsumption(otherCard.Id, 900m, ConsumptionStatus.Aprobado, createdAt: today));

            var result = await _consumptionRepository.GetAllAsync(
                1, 20,
                consumption => consumption.CreditCardId == card.Id,
                query => query.OrderByDescending(consumption => consumption.CreatedAt));

            result.TotalRecords.Should().Be(3);
            result.Items.Select(consumption => consumption.Amount).Should().Equal(200m, 300m, 100m);

            //Los rechazados permanecen en el historial con su motivo
            result.Items.Should().ContainSingle(consumption => consumption.Status == ConsumptionStatus.Rechazado)
                .Which.RejectionReason.Should().Be(RejectionReason.CreditoInsuficiente);
        }

        //Los avances de efectivo se ven en el mismo historial bajo el literal AVANCE.
        [Fact]
        public async Task GetAllAsync_ShouldKeepCashAdvancesInTheSameHistory()
        {
            var card = BuildCard("1111111111110001", CreditCardStatus.Activa);
            await GivenCreditCards(card);

            await GivenConsumptions(
                BuildConsumption(card.Id, 100m, ConsumptionStatus.Aprobado, commerceName: "Hermes Store"),
                BuildConsumption(card.Id, 1062.50m, ConsumptionStatus.Aprobado,
                    origin: ConsumptionOrigin.Avance, commerceName: "AVANCE"));

            var result = await _consumptionRepository.GetAllAsync(
                1, 20, consumption => consumption.CreditCardId == card.Id);

            result.Items.Should().Contain(consumption =>
                consumption.Origin == ConsumptionOrigin.Avance && consumption.CommerceName == "AVANCE");
        }

        [Fact]
        public async Task GetAllAsync_ShouldPageTheConsumptionsAtTwentyPerPage()
        {
            var card = BuildCard("1111111111110001", CreditCardStatus.Activa);
            await GivenCreditCards(card);

            var today = DateTimeOffset.UtcNow;

            await GivenConsumptions(Enumerable.Range(1, 23)
                .Select(number => BuildConsumption(
                    card.Id, number * 10m, ConsumptionStatus.Aprobado,
                    createdAt: today.AddMinutes(-number)))
                .ToArray());

            var firstPage = await _consumptionRepository.GetAllAsync(
                1, 50, consumption => consumption.CreditCardId == card.Id);

            var secondPage = await _consumptionRepository.GetAllAsync(
                2, 20, consumption => consumption.CreditCardId == card.Id);

            firstPage.Items.Should().HaveCount(20);
            firstPage.TotalRecords.Should().Be(23);
            secondPage.Items.Should().HaveCount(3);
        }
        #endregion

        public void Dispose() => _context.Dispose();

        #region helpers
        private async Task GivenCreditCards(params CreditCard[] creditCards)
        {
            await _context.CreditCards.AddRangeAsync(creditCards);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private async Task GivenConsumptions(params CardConsumption[] consumptions)
        {
            await _context.CardConsumptions.AddRangeAsync(consumptions);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static CreditCard[] BuildManyActiveCards(int total)
            => Enumerable.Range(1, total)
                .Select(number => BuildCard(
                    $"11111111111{number:D5}",
                    CreditCardStatus.Activa,
                    createdAt: DateTimeOffset.UtcNow.AddDays(-number)))
                .ToArray();

        private static CreditCard BuildCard(
            string cardNumber,
            CreditCardStatus status,
            string customerId = CustomerId,
            decimal owedAmount = 0m,
            DateTimeOffset? createdAt = null)
            => new()
            {
                CardNumber = cardNumber,
                LastFourDigits = cardNumber[^4..],
                CustomerId = customerId,
                CreditLimit = 50_000m,
                OwedAmount = owedAmount,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CvcHash = new string('a', 64),
                Status = status,
                AssignedByAdminId = "admin-1",
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        private static CardConsumption BuildConsumption(
            int creditCardId,
            decimal amount,
            ConsumptionStatus status,
            RejectionReason? rejectionReason = null,
            ConsumptionOrigin origin = ConsumptionOrigin.Comercio,
            string commerceName = "Comercio Artemis",
            DateTimeOffset? createdAt = null)
            => new()
            {
                CreditCardId = creditCardId,
                Amount = amount,
                Origin = origin,
                CommerceName = commerceName,
                Status = status,
                RejectionReason = rejectionReason,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "SYSTEM"
            };
        #endregion
    }
}