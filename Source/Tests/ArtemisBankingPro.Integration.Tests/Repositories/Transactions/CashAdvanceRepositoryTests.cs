using System;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Transactions
{
    public sealed class CashAdvanceRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-123";
        private readonly DbContextArtemisBanking _context;
        private readonly CashAdvanceRepository _repository;

        public CashAdvanceRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"cash-advances-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new CashAdvanceRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistCashAdvanceToDatabase()
        {
            var cashAdvance = new CashAdvance
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                RequestedAmount = 1000m,
                InterestRate = 0.0625m,
                InterestAmount = 62.50m,
                TotalCharged = 1062.50m,
                CardConsumptionId = 10,
                TransactionId = 20,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = CustomerId
            };

            await _repository.AddAsync(cashAdvance);
            await _repository.SaveChangesAsync();

            var dbAdvance = await _context.CashAdvances.FindAsync(cashAdvance.Id);
            dbAdvance.Should().NotBeNull();
            dbAdvance!.RequestedAmount.Should().Be(1000m);
            dbAdvance.InterestAmount.Should().Be(62.50m);
            dbAdvance.TotalCharged.Should().Be(1062.50m);
        }

        public void Dispose() => _context.Dispose();
    }
}
