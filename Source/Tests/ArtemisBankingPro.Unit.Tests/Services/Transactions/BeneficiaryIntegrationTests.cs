using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Beneficiaries;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Beneficiaries;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public sealed class BeneficiaryIntegrationTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;

        public BeneficiaryIntegrationTests()
        {
            _userManagementServiceMock = new Mock<IUserManagementService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<BeneficiaryMappingProfile>();
                cfg.AddProfile<BeneficiaryDtoToViewModelProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();
        }

        private DbContextArtemisBanking CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new DbContextArtemisBanking(options);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldPersistBeneficiary()
        {
            var dbName = $"beneficiary-integration-{Guid.NewGuid()}";
            var ownerId = "owner-123";
            var beneficiaryOwnerId = "ben-owner-456";

            int destAccountId;

            using (var seedContext = CreateContext(dbName))
            {
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = beneficiaryOwnerId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = beneficiaryOwnerId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await seedContext.SavingsAccounts.AddAsync(destAcc);
                await seedContext.SaveChangesAsync();
                destAccountId = destAcc.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);

                var validationServices = new BeneficiaryValidationServices(
                    savingsRepo, benRepo, NullLogger<BeneficiaryValidationServices>.Instance, _userManagementServiceMock.Object);

                var beneficiaryService = new BeneficiaryServices(
                    benRepo,
                    validationServices,
                    _mapper,
                    NullLogger<BeneficiaryServices>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new SaveBeneficiaryDto
                {
                    OwnerClientId = ownerId,
                    AccountNumber = "100000002"
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(ownerId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(beneficiaryOwnerId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                var result = await beneficiaryService.CreateAsync(dto);

                result.IsValid.Should().BeTrue();

                var dbBeneficiaries = await execContext.Beneficiaries.ToListAsync();
                dbBeneficiaries.Should().HaveCount(1);
                dbBeneficiaries.First().OwnerClientId.Should().Be(ownerId);
                dbBeneficiaries.First().BeneficiaryAccountNumber.Should().Be("100000002");
                dbBeneficiaries.First().BeneficiarySavingsAccountId.Should().Be(destAccountId);
                dbBeneficiaries.First().IsActive.Should().BeTrue();
            }
        }

        [Fact]
        public async Task CreateAsync_WhenBeneficiaryClientInactive_ShouldReturnAccountNotFound()
        {
            var dbName = $"beneficiary-integration-{Guid.NewGuid()}";
            var ownerId = "owner-123";
            var beneficiaryOwnerId = "ben-owner-456";

            using (var seedContext = CreateContext(dbName))
            {
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = beneficiaryOwnerId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = beneficiaryOwnerId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await seedContext.SavingsAccounts.AddAsync(destAcc);
                await seedContext.SaveChangesAsync();
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);

                var validationServices = new BeneficiaryValidationServices(
                    savingsRepo, benRepo, NullLogger<BeneficiaryValidationServices>.Instance, _userManagementServiceMock.Object);

                var beneficiaryService = new BeneficiaryServices(
                    benRepo,
                    validationServices,
                    _mapper,
                    NullLogger<BeneficiaryServices>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new SaveBeneficiaryDto
                {
                    OwnerClientId = ownerId,
                    AccountNumber = "100000002"
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(ownerId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(beneficiaryOwnerId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = false });

                var result = await beneficiaryService.CreateAsync(dto);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(BeneficiaryError.AccountNotFound);
            }
        }

        [Fact]
        public async Task DeactivateAsync_WhenValid_ShouldMarkInactive()
        {
            var dbName = $"beneficiary-integration-{Guid.NewGuid()}";
            var ownerId = "owner-123";
            var beneficiaryOwnerId = "ben-owner-456";

            int beneficiaryId;

            using (var seedContext = CreateContext(dbName))
            {
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = beneficiaryOwnerId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = beneficiaryOwnerId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await seedContext.SavingsAccounts.AddAsync(destAcc);
                await seedContext.SaveChangesAsync();

                var beneficiary = new Beneficiary
                {
                    OwnerClientId = ownerId,
                    BeneficiaryAccountNumber = "100000002",
                    BeneficiarySavingsAccountId = destAcc.Id,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = ownerId
                };
                await seedContext.Beneficiaries.AddAsync(beneficiary);
                await seedContext.SaveChangesAsync();

                beneficiaryId = beneficiary.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);

                var validationServices = new BeneficiaryValidationServices(
                    savingsRepo, benRepo, NullLogger<BeneficiaryValidationServices>.Instance, _userManagementServiceMock.Object);

                var beneficiaryService = new BeneficiaryServices(
                    benRepo,
                    validationServices,
                    _mapper,
                    NullLogger<BeneficiaryServices>.Instance,
                    _userManagementServiceMock.Object);

                var result = await beneficiaryService.DeactivateAsync(beneficiaryId, ownerId);

                result.IsValid.Should().BeTrue();

                var dbBeneficiary = await execContext.Beneficiaries.FindAsync(beneficiaryId);
                dbBeneficiary!.IsActive.Should().BeFalse();
                dbBeneficiary.DeactivatedAt.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetClientBeneficiariesAsync_WhenQuery_ShouldResolveFullNames()
        {
            var dbName = $"beneficiary-integration-{Guid.NewGuid()}";
            var ownerId = "owner-123";
            var beneficiaryOwnerId = "ben-owner-456";

            using (var seedContext = CreateContext(dbName))
            {
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = beneficiaryOwnerId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = beneficiaryOwnerId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await seedContext.SavingsAccounts.AddAsync(destAcc);
                await seedContext.SaveChangesAsync();

                var beneficiary = new Beneficiary
                {
                    OwnerClientId = ownerId,
                    BeneficiaryAccountNumber = "100000002",
                    BeneficiarySavingsAccountId = destAcc.Id,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = ownerId
                };
                await seedContext.Beneficiaries.AddAsync(beneficiary);
                await seedContext.SaveChangesAsync();
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);

                var validationServices = new BeneficiaryValidationServices(
                    savingsRepo, benRepo, NullLogger<BeneficiaryValidationServices>.Instance, _userManagementServiceMock.Object);

                var beneficiaryService = new BeneficiaryServices(
                    benRepo,
                    validationServices,
                    _mapper,
                    NullLogger<BeneficiaryServices>.Instance,
                    _userManagementServiceMock.Object);

                _userManagementServiceMock.Setup(u => u.GetFullNameByIdAsync(beneficiaryOwnerId))
                    .ReturnsAsync("Maria Gomez");

                var result = await beneficiaryService.GetClientBeneficiariesAsync(ownerId);

                result.IsValid.Should().BeTrue();
                result.Value.Should().HaveCount(1);
                result.Value!.First().OwnerFullName.Should().Be("Maria Gomez");
            }
        }
    }
}
