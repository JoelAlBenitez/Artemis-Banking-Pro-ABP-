using System;
using System.Linq;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.SavingsAccounts
{
    public sealed class SavingsAccountsRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-123";
        private readonly DbContextArtemisBanking _context;
        private readonly SavingsAccountsRepository _repository;

        public SavingsAccountsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"savings-accounts-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new SavingsAccountsRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistSavingsAccountToDatabase()
        {
            var account = BuildAccount("100000001", SavingsAccountType.Principal, SavingsAccountStatus.Activa);

            await _repository.AddAsync(account);
            await _repository.SaveChangesAsync();

            var dbAccount = await _context.SavingsAccounts.FindAsync(account.Id);
            dbAccount.Should().NotBeNull();
            dbAccount!.AccountNumber.Should().Be("100000001");
            dbAccount.CustomerId.Should().Be(CustomerId);
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithoutFilters_ShouldReturnActiveFirstThenDescendingDate()
        {
            var today = DateTimeOffset.UtcNow;
            await GivenAccounts(
                BuildAccount("100000001", SavingsAccountType.Principal, SavingsAccountStatus.Cancelada, today.AddDays(-1)),
                BuildAccount("100000002", SavingsAccountType.Secundaria, SavingsAccountStatus.Activa, today.AddDays(-10)),
                BuildAccount("100000003", SavingsAccountType.Secundaria, SavingsAccountStatus.Cancelada, today.AddDays(-30)),
                BuildAccount("100000004", SavingsAccountType.Principal, SavingsAccountStatus.Activa, today.AddDays(-2))
            );

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, null);

            result.Items.Select(a => a.AccountNumber)
                .Should().Equal("100000004", "100000002", "100000001", "100000003");
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithStatusFilter_ShouldOnlyReturnMatchingStatus()
        {
            await GivenAccounts(
                BuildAccount("100000001", SavingsAccountType.Principal, SavingsAccountStatus.Activa),
                BuildAccount("100000002", SavingsAccountType.Secundaria, SavingsAccountStatus.Cancelada)
            );

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, SavingsAccountStatus.Activa, null, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(a => a.Status == SavingsAccountStatus.Activa);
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithAccountTypeFilter_ShouldOnlyReturnMatchingType()
        {
            await GivenAccounts(
                BuildAccount("100000001", SavingsAccountType.Principal, SavingsAccountStatus.Activa),
                BuildAccount("100000002", SavingsAccountType.Secundaria, SavingsAccountStatus.Activa)
            );

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, SavingsAccountType.Secundaria, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(a => a.AccountType == SavingsAccountType.Secundaria);
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithCustomerFilter_ShouldOnlyReturnForThatCustomer()
        {
            var otherAccount = BuildAccount("100000002", SavingsAccountType.Principal, SavingsAccountStatus.Activa);
            otherAccount.CustomerId = "other-customer";

            await GivenAccounts(
                BuildAccount("100000001", SavingsAccountType.Principal, SavingsAccountStatus.Activa),
                otherAccount
            );

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, CustomerId);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(a => a.CustomerId == CustomerId);
        }

        public void Dispose() => _context.Dispose();

        private async Task GivenAccounts(params SavingsAccount[] accounts)
        {
            await _context.SavingsAccounts.AddRangeAsync(accounts);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static SavingsAccount BuildAccount(string accountNumber, SavingsAccountType type, SavingsAccountStatus status, DateTimeOffset? createdAt = null)
            => new()
            {
                AccountNumber = accountNumber,
                CustomerId = CustomerId,
                AccountType = type,
                Status = status,
                Balance = 1000m,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };
    }
}
