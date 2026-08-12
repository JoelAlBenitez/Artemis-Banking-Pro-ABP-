using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Commerces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Commerces
{
    //Proyección de consulta de Hermes Pay: guarda el pago recibido para no tener que cruzar
    //tarjetas, consumos y cuentas en cada llamada.
    public sealed class CommercePaymentRepositoryTests : IDisposable
    {
        private const int CommerceId = 5;
        private const int OtherCommerceId = 9;

        private readonly DbContextArtemisBanking _context;
        private readonly CommercePaymentRepository _repository;

        public CommercePaymentRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"commerce-payments-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new CommercePaymentRepository(_context);
        }

        #region orden y aislamiento por comercio

        [Fact]
        public async Task GetPagedPaymentsByCommerceAsync_ShouldOrderFromTheNewestToTheOldest()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenPayments(
                BuildPayment(amount: 100m, createdAt: today.AddDays(-5)),
                BuildPayment(amount: 200m, createdAt: today),
                BuildPayment(amount: 300m, createdAt: today.AddDays(-2)));

            var result = await _repository.GetPagedPaymentsByCommerceAsync(CommerceId, 1, 20);

            result.Items.Select(payment => payment.Amount).Should().Equal(200m, 300m, 100m);
        }

        //Un comercio nunca puede ver los pagos de otro.
        [Fact]
        public async Task GetPagedPaymentsByCommerceAsync_ShouldOnlyReturnThePaymentsOfTheGivenCommerce()
        {
            await GivenPayments(
                BuildPayment(amount: 100m),
                BuildPayment(amount: 200m, commerceId: OtherCommerceId),
                BuildPayment(amount: 300m));

            var result = await _repository.GetPagedPaymentsByCommerceAsync(CommerceId, 1, 20);

            result.TotalRecords.Should().Be(2);
            result.Items.Should().OnlyContain(payment => payment.CommerceId == CommerceId);
        }

        [Fact]
        public async Task GetPagedPaymentsByCommerceAsync_WithoutPayments_ShouldReturnAnEmptyPage()
        {
            var result = await _repository.GetPagedPaymentsByCommerceAsync(CommerceId, 1, 20);

            result.Items.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        #endregion

        #region paginación

        [Fact]
        public async Task GetPagedPaymentsByCommerceAsync_WithPageSizeAboveTheMaximum_ShouldClampItToTwenty()
        {
            var payments = Enumerable.Range(1, 25)
                .Select(index => BuildPayment(amount: index))
                .ToArray();

            await GivenPayments(payments);

            var result = await _repository.GetPagedPaymentsByCommerceAsync(CommerceId, 1, 50);

            result.Items.Should().HaveCount(20);
            result.PageSize.Should().Be(20);
            result.TotalPages.Should().Be(2);
        }

        #endregion

        #region persistencia

        //Los rechazados se conservan como evidencia y no llevan transacción asociada.
        [Fact]
        public async Task AddAsync_ShouldPersistARejectedPaymentWithoutTransaction()
        {
            var payment = BuildPayment(amount: 201m, status: ConsumptionStatus.Rechazado);
            payment.TransactionId = null;

            await _repository.AddAsync(payment);
            await _repository.SaveChangesAsync();

            var stored = await _context.CommercePayments.SingleAsync();
            stored.Status.Should().Be(ConsumptionStatus.Rechazado);
            stored.TransactionId.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldPersistAnApprovedPaymentLinkedToItsTransaction()
        {
            var payment = BuildPayment(amount: 689.25m);
            payment.TransactionId = 77;

            await _repository.AddAsync(payment);
            await _repository.SaveChangesAsync();

            var stored = await _context.CommercePayments.SingleAsync();
            stored.Status.Should().Be(ConsumptionStatus.Aprobado);
            stored.TransactionId.Should().Be(77);
            stored.CardConsumptionId.Should().Be(11);
        }

        //Único dato de la tarjeta que la proyección puede guardar.
        [Fact]
        public async Task AddAsync_ShouldOnlyKeepTheLastFourDigitsOfTheCard()
        {
            await _repository.AddAsync(BuildPayment(amount: 100m));
            await _repository.SaveChangesAsync();

            var stored = await _context.CommercePayments.SingleAsync();
            stored.CardLastFourDigits.Should().Be("7598");
            stored.CardLastFourDigits.Should().HaveLength(4);
        }

        #endregion

        #region builders

        private async Task GivenPayments(params CommercePayment[] payments)
        {
            await _context.CommercePayments.AddRangeAsync(payments);
            await _context.SaveChangesAsync();
        }

        private static CommercePayment BuildPayment(
            decimal amount,
            int commerceId = CommerceId,
            ConsumptionStatus status = ConsumptionStatus.Aprobado,
            DateTimeOffset? createdAt = null)
            => new()
            {
                CommerceId = commerceId,
                CreditCardId = 1,
                CardLastFourDigits = "7598",
                Amount = amount,
                CardConsumptionId = 11,
                TransactionId = status == ConsumptionStatus.Aprobado ? 77 : null,
                Status = status,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "usuario-comercio"
            };

        #endregion

        public void Dispose() => _context.Dispose();
    }
}
