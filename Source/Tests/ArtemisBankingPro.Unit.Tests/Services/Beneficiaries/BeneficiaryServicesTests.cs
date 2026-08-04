using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Beneficiaries;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Beneficiaries
{
    public sealed class BeneficiaryServicesTests
    {
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<BeneficiaryServices>> _loggerMock;
        private readonly BeneficiaryServices _beneficiaryServices;

        public BeneficiaryServicesTests()
        {
            _beneficiaryRepositoryMock = new Mock<IBeneficiaryRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<BeneficiaryServices>>();

            _beneficiaryServices = new BeneficiaryServices(
                _beneficiaryRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentAccount_ShouldReturnFailureAndAccountNotFound()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "999999999"
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync((SavingsAccount)null!);

            var result = await _beneficiaryServices.CreateAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AccountNotFound);
        }

        [Fact]
        public async Task CreateAsync_WithCanceledAccount_ShouldReturnFailureAndAccountCanceled()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "111111111"
            };

            var account = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "111111111",
                CustomerId = "other-1",
                Status = SavingsAccountStatus.Cancelada,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _beneficiaryServices.CreateAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AccountCanceled);
        }

        [Fact]
        public async Task CreateAsync_WithOwnAccount_ShouldReturnFailureAndOwnAccount()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "222222222"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "222222222",
                CustomerId = "owner-1",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _beneficiaryServices.CreateAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.OwnAccount);
        }

        [Fact]
        public async Task CreateAsync_WithAlreadyRegisteredAccount_ShouldReturnFailureAndAlreadyRegistered()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "333333333"
            };

            var account = new SavingsAccount
            {
                Id = 3,
                AccountNumber = "333333333",
                CustomerId = "other-2",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            _beneficiaryRepositoryMock.Setup(r => r.ExistElementByConsult(
                It.IsAny<Expression<Func<Beneficiary, bool>>>()
            )).ReturnsAsync(true);

            var result = await _beneficiaryServices.CreateAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AlreadyRegistered);
        }

        [Fact]
        public async Task CreateAsync_WithValidAccount_ShouldCreateBeneficiaryAndReturnSuccess()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "444444444"
            };

            var account = new SavingsAccount
            {
                Id = 4,
                AccountNumber = "444444444",
                CustomerId = "other-3",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            _beneficiaryRepositoryMock.Setup(r => r.ExistElementByConsult(
                It.IsAny<Expression<Func<Beneficiary, bool>>>()
            )).ReturnsAsync(false);

            _beneficiaryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Beneficiary>()))
                .ReturnsAsync((Beneficiary b) => b);

            _beneficiaryRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _beneficiaryServices.CreateAsync(dto);

            result.IsValid.Should().BeTrue();
            _beneficiaryRepositoryMock.Verify(r => r.AddAsync(It.Is<Beneficiary>(b => 
                b.OwnerClientId == "owner-1" && 
                b.BeneficiarySavingsAccountId == 4 && 
                b.BeneficiaryAccountNumber == "444444444" && 
                b.IsActive
            )), Times.Once);
            _beneficiaryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeactivateAsync_WithExistingBeneficiary_ShouldSetIsActiveToFalseAndReturnSuccess()
        {
            var beneficiary = new Beneficiary
            {
                Id = 10,
                OwnerClientId = "owner-1",
                BeneficiarySavingsAccountId = 5,
                BeneficiaryAccountNumber = "555555555",
                IsActive = true,
                CreateByUserId = "owner-1",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _beneficiaryRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<Beneficiary, bool>>>(),
                It.IsAny<Expression<Func<Beneficiary, object>>[]>()
            )).ReturnsAsync(beneficiary);

            _beneficiaryRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Beneficiary>()))
                .ReturnsAsync(true);

            _beneficiaryRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _beneficiaryServices.DeactivateAsync(10, "owner-1");

            result.IsValid.Should().BeTrue();
            beneficiary.IsActive.Should().BeFalse();
            beneficiary.DeactivatedAt.Should().NotBeNull();
            beneficiary.LastModifiedByIdUser.Should().Be("owner-1");

            _beneficiaryRepositoryMock.Verify(r => r.UpdateAsync(beneficiary), Times.Once);
            _beneficiaryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
