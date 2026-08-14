using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Beneficiaries;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using ArtemisBankingPro.Core.Application.Contracts.Users.Management;

namespace ArtemisBankingPro.Unit.Tests.Services.Beneficiaries
{
    public sealed class BeneficiaryServicesTests
    {
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepositoryMock;
        private readonly Mock<IBeneficiaryValidationServices> _beneficiaryValidationServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<BeneficiaryServices>> _loggerMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly BeneficiaryServices _beneficiaryServices;

        public BeneficiaryServicesTests()
        {
            _beneficiaryRepositoryMock = new Mock<IBeneficiaryRepository>();
            _beneficiaryValidationServicesMock = new Mock<IBeneficiaryValidationServices>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<BeneficiaryServices>>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            _beneficiaryServices = new BeneficiaryServices(
                _beneficiaryRepositoryMock.Object,
                _beneficiaryValidationServicesMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _userManagementServiceMock.Object
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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateCreationAsync(It.IsAny<SaveBeneficiaryDto>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound));

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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateCreationAsync(It.IsAny<SaveBeneficiaryDto>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountCanceled));

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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateCreationAsync(It.IsAny<SaveBeneficiaryDto>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Failure(BeneficiaryError.OwnAccount));

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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateCreationAsync(It.IsAny<SaveBeneficiaryDto>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AlreadyRegistered));

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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateCreationAsync(It.IsAny<SaveBeneficiaryDto>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Success(account));

            _mapperMock.Setup(m => m.Map<Beneficiary>(It.IsAny<SaveBeneficiaryDto>()))
                .Returns((SaveBeneficiaryDto s) => new Beneficiary
                {
                    OwnerClientId = s.OwnerClientId,
                    BeneficiaryAccountNumber = s.AccountNumber,
                    BeneficiarySavingsAccountId = 0,
                    IsActive = true,
                    CreateByUserId = s.OwnerClientId,
                    CreatedAt = DateTimeOffset.UtcNow
                });

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

            _beneficiaryValidationServicesMock.Setup(r => r.ValidateDeactivationAsync(10, "owner-1"))
                .ReturnsAsync(ValidationResult<Beneficiary>.Success(beneficiary));

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
