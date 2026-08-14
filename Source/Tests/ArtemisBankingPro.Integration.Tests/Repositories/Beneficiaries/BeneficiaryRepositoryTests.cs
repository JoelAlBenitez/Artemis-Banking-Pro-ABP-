using System;
using System.Linq;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Beneficiaries;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Beneficiaries
{
    public sealed class BeneficiaryRepositoryTests : IDisposable
    {
        private const string OwnerId = "owner-123";
        private readonly DbContextArtemisBanking _context;
        private readonly BeneficiaryRepository _repository;

        public BeneficiaryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"beneficiaries-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new BeneficiaryRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistBeneficiaryToDatabase()
        {
            var beneficiary = BuildBeneficiary("100000001", 1);

            await _repository.AddAsync(beneficiary);
            await _repository.SaveChangesAsync();

            var dbBeneficiary = await _context.Beneficiaries.FindAsync(beneficiary.Id);
            dbBeneficiary.Should().NotBeNull();
            dbBeneficiary!.OwnerClientId.Should().Be(OwnerId);
            dbBeneficiary.BeneficiaryAccountNumber.Should().Be("100000001");
            dbBeneficiary.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingBeneficiary()
        {
            var beneficiary = BuildBeneficiary("100000001", 1);
            await _repository.AddAsync(beneficiary);
            await _repository.SaveChangesAsync();

            beneficiary.IsActive = false;
            beneficiary.DeactivatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(beneficiary);
            await _repository.SaveChangesAsync();

            var dbBeneficiary = await _context.Beneficiaries.FindAsync(beneficiary.Id);
            dbBeneficiary!.IsActive.Should().BeFalse();
            dbBeneficiary.DeactivatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllMatchingBeneficiaries()
        {
            await GivenBeneficiaries(
                BuildBeneficiary("100000001", 1),
                BuildBeneficiary("100000002", 2)
            );

            var result = await _repository.GetAllAsync(1, 10);
            result.Items.Should().HaveCount(2);
        }

        public void Dispose() => _context.Dispose();

        private async Task GivenBeneficiaries(params Beneficiary[] beneficiaries)
        {
            await _context.Beneficiaries.AddRangeAsync(beneficiaries);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static Beneficiary BuildBeneficiary(string accountNumber, int accountId)
            => new()
            {
                OwnerClientId = OwnerId,
                BeneficiaryAccountNumber = accountNumber,
                BeneficiarySavingsAccountId = accountId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = OwnerId
            };
    }
}
