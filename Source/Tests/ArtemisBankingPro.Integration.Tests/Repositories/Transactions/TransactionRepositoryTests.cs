using System;
using System.Linq;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Transactions
{
    public sealed class TransactionRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-123";
        private readonly DbContextArtemisBanking _context;
        private readonly TransactionRepository _repository;

        public TransactionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"transactions-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new TransactionRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistTransactionToDatabase()
        {
            var transaction = BuildTransaction(100m, OperationType.TransferenciaEntreCuentas, TransactionStatus.Aprobada);

            await _repository.AddAsync(transaction);
            await _repository.SaveChangesAsync();

            var dbTransaction = await _context.Transactions.FindAsync(transaction.Id);
            dbTransaction.Should().NotBeNull();
            dbTransaction!.Amount.Should().Be(100m);
            dbTransaction.OperationType.Should().Be(OperationType.TransferenciaEntreCuentas);
        }

        [Fact]
        public async Task GetTotalHistoricalAsync_ShouldReturnCountOfAllTransactions()
        {
            await GivenTransactions(
                BuildTransaction(100m, OperationType.TransferenciaEntreCuentas, TransactionStatus.Aprobada),
                BuildTransaction(200m, OperationType.TransaccionExpress, TransactionStatus.Rechazada)
            );

            var total = await _repository.GetTotalHistoricalAsync();
            total.Should().Be(2);
        }

        [Fact]
        public async Task GetTotalTodayAsync_ShouldReturnCountOfTransactionsCreatedToday()
        {
            var today = DateTimeOffset.UtcNow;
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            await GivenTransactions(
                BuildTransaction(100m, OperationType.TransferenciaEntreCuentas, TransactionStatus.Aprobada, today),
                BuildTransaction(200m, OperationType.TransaccionExpress, TransactionStatus.Aprobada, yesterday)
            );

            var total = await _repository.GetTotalTodayAsync();
            total.Should().Be(1);
        }

        [Fact]
        public async Task GetPaymentsAsync_ShouldReturnApprovedPaymentsFilteredByChannelAndDate()
        {
            var today = DateTimeOffset.UtcNow;
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            await GivenTransactions(
                BuildTransaction(100m, OperationType.PagoTarjeta, TransactionStatus.Aprobada, today, ChannelPayment.Cajero),
                BuildTransaction(200m, OperationType.PagoPrestamo, TransactionStatus.Aprobada, today, ChannelPayment.Cliente),
                BuildTransaction(300m, OperationType.PagoTarjeta, TransactionStatus.Rechazada, today, ChannelPayment.Cajero), // Rechazada
                BuildTransaction(400m, OperationType.TransaccionExpress, TransactionStatus.Aprobada, today, ChannelPayment.Cajero), // No es pago
                BuildTransaction(500m, OperationType.PagoTarjeta, TransactionStatus.Aprobada, yesterday, ChannelPayment.Cajero) // Ayer
            );

            var resultAllTodayCajero = await _repository.GetPaymentsAsync(ChannelPayment.Cajero, today);
            resultAllTodayCajero.Should().HaveCount(1);
            resultAllTodayCajero.First().Amount.Should().Be(100m);

            var resultAllCajero = await _repository.GetPaymentsAsync(ChannelPayment.Cajero, null);
            resultAllCajero.Should().HaveCount(2);

            var resultAllToday = await _repository.GetPaymentsAsync(null, today);
            resultAllToday.Should().HaveCount(2);
        }

        public void Dispose() => _context.Dispose();

        private async Task GivenTransactions(params Transaction[] transactions)
        {
            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static Transaction BuildTransaction(
            decimal amount,
            OperationType opType,
            TransactionStatus status,
            DateTimeOffset? createdAt = null,
            ChannelPayment channel = ChannelPayment.Cliente)
            => new()
            {
                Amount = amount,
                OperationType = opType,
                TransactionType = TransactionType.Debito,
                Status = status,
                Channel = channel,
                SavingsAccountId = 1,
                Origin = "Internet Banking",
                PerformedByUserId = CustomerId,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = CustomerId
            };
    }
}
