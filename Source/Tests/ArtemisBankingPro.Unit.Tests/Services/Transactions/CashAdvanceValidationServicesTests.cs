using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public sealed class CashAdvanceValidationServicesTests
    {
        private readonly Mock<ICreditCardsRepository> _creditCardsRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<ICardConsumptionRepository> _cardConsumptionRepositoryMock;
        private readonly Mock<ILogger<CashAdvanceValidationServices>> _loggerMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly CashAdvanceValidationServices _validationServices;

        public CashAdvanceValidationServicesTests()
        {
            _creditCardsRepositoryMock = new Mock<ICreditCardsRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _cardConsumptionRepositoryMock = new Mock<ICardConsumptionRepository>();
            _loggerMock = new Mock<ILogger<CashAdvanceValidationServices>>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

            _validationServices = new CashAdvanceValidationServices(
                _creditCardsRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object,
                _cardConsumptionRepositoryMock.Object,
                _loggerMock.Object,
                _userManagementServiceMock.Object
            );
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithAmountLessThanOrEqualToZero_ShouldReturnAmountInvalid()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 1,
                Amount = 0
            };

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.AmountInvalid);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithNonExistentCard_ShouldReturnCardNotActive()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 99,
                SavingsAccountId = 1,
                Amount = 100
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync((CreditCard)null!);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.CardNotActive);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithInactiveCard_ShouldRegisterRejectedConsumptionAndReturnCardNotActive()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 1,
                Amount = 100
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 1000,
                OwedAmount = 0,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Cancelada,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync(card);

            _cardConsumptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CardConsumption>()))
                .ReturnsAsync((CardConsumption c) => c);

            _cardConsumptionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.CardNotActive);

            _cardConsumptionRepositoryMock.Verify(r => r.AddAsync(It.Is<CardConsumption>(c => 
                c.CreditCardId == 1 &&
                c.Status == ConsumptionStatus.Rechazado &&
                c.RejectionReason == RejectionReason.TarjetaCancelada &&
                c.Amount == 106.25m
            )), Times.Once);
            _cardConsumptionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithExpiredCard_ShouldRegisterRejectedConsumptionAndReturnCardExpired()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 1,
                Amount = 100
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 1000,
                OwedAmount = 0,
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(-1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync(card);

            _cardConsumptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CardConsumption>()))
                .ReturnsAsync((CardConsumption c) => c);

            _cardConsumptionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.CardExpired);

            _cardConsumptionRepositoryMock.Verify(r => r.AddAsync(It.Is<CardConsumption>(c => 
                c.CreditCardId == 1 &&
                c.Status == ConsumptionStatus.Rechazado &&
                c.RejectionReason == RejectionReason.TarjetaVencida &&
                c.Amount == 106.25m
            )), Times.Once);
            _cardConsumptionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithInactiveAccount_ShouldReturnAccountNotActive()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                Amount = 100
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 1000,
                OwedAmount = 0,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "123456789",
                CustomerId = "client-1",
                Balance = 500,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Cancelada,
                CreateByUserId = "admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync(card);

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.AccountNotActive);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithInsufficientCredit_ShouldRegisterRejectedConsumptionAndReturnInsufficientCredit()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                Amount = 200
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 500,
                OwedAmount = 300, // Available: 200. Total charged for 200 requested is 212.50
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "123456789",
                CustomerId = "client-1",
                Balance = 500,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Activa,
                CreateByUserId = "admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync(card);

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            _cardConsumptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CardConsumption>()))
                .ReturnsAsync((CardConsumption c) => c);

            _cardConsumptionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.InsufficientCredit);

            _cardConsumptionRepositoryMock.Verify(r => r.AddAsync(It.Is<CardConsumption>(c => 
                c.CreditCardId == 1 &&
                c.Status == ConsumptionStatus.Rechazado &&
                c.RejectionReason == RejectionReason.CreditoInsuficiente &&
                c.Amount == 212.50m
            )), Times.Once);
            _cardConsumptionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateCashAdvanceAsync_WithValidInputs_ShouldReturnSuccess()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                Amount = 100
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 1000,
                OwedAmount = 0,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "123456789",
                CustomerId = "client-1",
                Balance = 500,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Activa,
                CreateByUserId = "admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _creditCardsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<CreditCard, bool>>>(),
                It.IsAny<Expression<Func<CreditCard, object>>[]>()
            )).ReturnsAsync(card);

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _validationServices.ValidateCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeTrue();
            result.Value.Card.Should().Be(card);
            result.Value.Account.Should().Be(account);
            result.Value.InterestAmount.Should().Be(6.25m);
            result.Value.TotalCharged.Should().Be(106.25m);
        }
    }
}
