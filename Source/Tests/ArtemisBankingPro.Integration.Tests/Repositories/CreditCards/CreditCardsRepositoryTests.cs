using System;
using System.Linq;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.CreditCards
{
    public sealed class CreditCardsRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-123";
        private readonly DbContextArtemisBanking _context;
        private readonly CreditCardsRepository _repository;

        public CreditCardsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"credit-cards-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new CreditCardsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistCreditCardToDatabase()
        {
            var card = BuildCard("1234567890123456", CreditCardStatus.Activa);

            await _repository.AddAsync(card);
            await _repository.SaveChangesAsync();

            var dbCard = await _context.CreditCards.FindAsync(card.Id);
            dbCard.Should().NotBeNull();
            dbCard!.CardNumber.Should().Be("1234567890123456");
            dbCard.CustomerId.Should().Be(CustomerId);
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_WithoutFilters_ShouldReturnActiveFirstThenDescendingDate()
        {
            var today = DateTimeOffset.UtcNow;
            await GivenCards(
                BuildCard("1111222233334444", CreditCardStatus.Cancelada, today.AddDays(-1)),
                BuildCard("2222333344445555", CreditCardStatus.Activa, today.AddDays(-10)),
                BuildCard("3333444455556666", CreditCardStatus.Cancelada, today.AddDays(-30)),
                BuildCard("4444555566667777", CreditCardStatus.Activa, today.AddDays(-2))
            );

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, null);

            result.Items.Select(c => c.CardNumber)
                .Should().Equal("4444555566667777", "2222333344445555", "1111222233334444", "3333444455556666");
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_WithStatusFilter_ShouldOnlyReturnMatchingStatus()
        {
            await GivenCards(
                BuildCard("1111222233334444", CreditCardStatus.Activa),
                BuildCard("2222333344445555", CreditCardStatus.Cancelada)
            );

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, CreditCardStatus.Activa, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(c => c.Status == CreditCardStatus.Activa);
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_WithCustomerFilter_ShouldOnlyReturnForThatCustomer()
        {
            var otherCard = BuildCard("2222333344445555", CreditCardStatus.Activa);
            otherCard.CustomerId = "other-customer";

            await GivenCards(
                BuildCard("1111222233334444", CreditCardStatus.Activa),
                otherCard
            );

            var result = await _repository.GetPagedCreditCardsAsync(1, 20, null, CustomerId);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(c => c.CustomerId == CustomerId);
        }

        public void Dispose() => _context.Dispose();

        private async Task GivenCards(params CreditCard[] cards)
        {
            await _context.CreditCards.AddRangeAsync(cards);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static CreditCard BuildCard(string cardNumber, CreditCardStatus status, DateTimeOffset? createdAt = null)
            => new()
            {
                CardNumber = cardNumber,
                LastFourDigits = cardNumber.Substring(cardNumber.Length - 4),
                CustomerId = CustomerId,
                CreditLimit = 5000m,
                OwedAmount = 0m,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CvcHash = "hash",
                Status = status,
                AssignedByAdminId = "admin",
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };
    }
}
