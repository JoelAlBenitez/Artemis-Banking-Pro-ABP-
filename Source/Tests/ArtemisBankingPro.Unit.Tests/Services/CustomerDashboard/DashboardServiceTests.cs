using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Dashboard;
using Artemis_Banking_Pro.Core.Application.ViewModels.Dashboard;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.CustomerDashboard
{
    public class DashboardServiceTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ICreditCardsRepository> _creditCardRepositoryMock;
        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<ICardConsumptionRepository> _cardConsumptionRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _creditCardRepositoryMock = new Mock<ICreditCardsRepository>();
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _cardConsumptionRepositoryMock = new Mock<ICardConsumptionRepository>();
            _mapperMock = new Mock<IMapper>();

            _dashboardService = new DashboardService(
                _savingsAccountRepositoryMock.Object,
                _creditCardRepositoryMock.Object,
                _loansRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _cardConsumptionRepositoryMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public async Task GetClientDashboardAsync_WithActiveProducts_ShouldSortAccountsAndMapToViewModel()
        {
            var clientId = "client-123";

            var accounts = new List<SavingsAccount>
            {
                new() { Id = 1, AccountNumber = "A1", CustomerId = clientId, Balance = 100, Status = SavingsAccountStatus.Activa, AccountType = SavingsAccountType.Secundaria, CreatedAt = DateTimeOffset.UtcNow, CreateByUserId = clientId },
                new() { Id = 2, AccountNumber = "A2", CustomerId = clientId, Balance = 500, Status = SavingsAccountStatus.Activa, AccountType = SavingsAccountType.Principal, CreatedAt = DateTimeOffset.UtcNow, CreateByUserId = clientId }
            };

            var loans = new List<Loan>
            {
                new() { Id = 1, LoanNumber = "L1", CustomerId = clientId, ApprovedCapital = 10000m, termMonths = TermMonths.Meses12, AnnualInterestRate = 0.15m, Status = LoanStatus.Activo, CreatedAt = DateTimeOffset.UtcNow, CreateByUserId = clientId }
            };

            var cards = new List<CreditCard>
            {
                new() { Id = 1, CardNumber = "C1", LastFourDigits = "1234", CustomerId = clientId, CreditLimit = 1000m, ExpirationDate = DateTimeOffset.UtcNow.AddYears(1), CvcHash = "hash", Status = CreditCardStatus.Activa, AssignedByAdminId = "admin", CreatedAt = DateTimeOffset.UtcNow, CreateByUserId = clientId }
            };

            _savingsAccountRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(accounts);

            _loansRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<Loan, bool>>>(), It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync(loans);

            _creditCardRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<CreditCard, bool>>>(), It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync(cards);

            var accountsMapped = new List<SavingsAccountDto> 
            { 
                new() 
                { 
                    Id = 2, 
                    AccountNumber = "A2", 
                    CustomerId = clientId, 
                    FullNameCustomer = "John Doe", 
                    IdCard = "123", 
                    Balance = 500m, 
                    TypeSavingsAccount = SavingsAccountType.Principal, 
                    StateSavingsAccount = SavingsAccountStatus.Activa 
                },
                new() 
                { 
                    Id = 1, 
                    AccountNumber = "A1", 
                    CustomerId = clientId, 
                    FullNameCustomer = "John Doe", 
                    IdCard = "123", 
                    Balance = 100m, 
                    TypeSavingsAccount = SavingsAccountType.Secundaria, 
                    StateSavingsAccount = SavingsAccountStatus.Activa 
                }
            };
            var loansMapped = new List<LoansDto> 
            { 
                new() 
                { 
                    Id = 1, 
                    LoanNumber = "L1", 
                    CustomerId = clientId, 
                    FullNameCustomer = "John Doe", 
                    AprovechedCapital = 10000m, 
                    QuantityInstallment = 12, 
                    InstallmentPay = 0, 
                    PendientAmount = 10000m, 
                    AnnualInterestRate = 0.15m, 
                    Term = 12, 
                    StateLoans = LoanStatus.Activo, 
                    CustomerInArrears = false 
                } 
            };
            var cardsMapped = new List<CreditCardDto> 
            { 
                new() 
                { 
                    Id = 1, 
                    MaskedCardNumber = "C1", 
                    LastFourDigits = "1234", 
                    CustomerId = clientId, 
                    FullNameCustomer = "John Doe", 
                    CreditLimit = 1000m, 
                    ExpirationDate = "12/27", 
                    OwedAmount = 0, 
                    AvailableCredit = 1000m, 
                    StateCreditCard = CreditCardStatus.Activa 
                } 
            };

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<SavingsAccountDto>>(It.IsAny<List<SavingsAccount>>())).Returns(accountsMapped);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<LoansDto>>(It.IsAny<List<Loan>>())).Returns(loansMapped);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<CreditCardDto>>(It.IsAny<List<CreditCard>>())).Returns(cardsMapped);

            var result = await _dashboardService.GetClientDashboardAsync(clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.SavingsAccounts.Should().HaveCount(2);
            result.Value.Loans.Should().HaveCount(1);
            result.Value.CreditCards.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetClientDashboardAsync_OnException_ShouldReturnFailure()
        {
            var clientId = "client-123";

            _savingsAccountRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ThrowsAsync(new Exception("DB connection failed"));

            var result = await _dashboardService.GetClientDashboardAsync(clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Description == "Ha ocurrido un error al cargar el dashboard del cliente.");
        }

        [Fact]
        public async Task GetSavingsAccountDetailsAsync_WithValidAccount_ShouldReturnMappedTransactions()
        {
            var accountId = 1;
            var clientId = "client-123";

            var account = new SavingsAccount
            {
                Id = accountId,
                AccountNumber = "ACC-123",
                CustomerId = clientId,
                Balance = 500,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var transactions = new List<Transaction>
            {
                new() { Id = 10, SavingsAccountId = accountId, Amount = 100, TransactionType = TransactionType.Credito, OperationType = OperationType.Deposito, Origin = "ATM", Status = TransactionStatus.Aprobada, Channel = ChannelPayment.Cajero, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), PerformedByUserId = clientId, CreateByUserId = clientId },
                new() { Id = 11, SavingsAccountId = accountId, Amount = 50, TransactionType = TransactionType.Debito, OperationType = OperationType.TransaccionExpress, Origin = "ACC-123", Status = TransactionStatus.Aprobada, Channel = ChannelPayment.Cliente, CreatedAt = DateTimeOffset.UtcNow, PerformedByUserId = clientId, CreateByUserId = clientId }
            };

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(account);

            _transactionRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<Transaction, bool>>>(), It.IsAny<Expression<Func<Transaction, object>>[]>()))
                .ReturnsAsync(transactions);

            var mappedTxs = new List<TransactionResultDto>
            {
                new() { EffectiveAmount = 50, TransactionType = TransactionType.Debito },
                new() { EffectiveAmount = 100, TransactionType = TransactionType.Credito }
            };

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<TransactionResultDto>>(It.IsAny<List<Transaction>>())).Returns(mappedTxs);

            var result = await _dashboardService.GetSavingsAccountDetailsAsync(accountId, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.First().TransactionType.Should().Be(TransactionType.Debito);
        }

        [Fact]
        public async Task GetSavingsAccountDetailsAsync_WithAccountNotFoundOrUnowned_ShouldReturnAccountNotFoundFailure()
        {
            var accountId = 1;
            var clientId = "client-123";

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((SavingsAccount)null!);

            var result = await _dashboardService.GetSavingsAccountDetailsAsync(accountId, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(DashboardError.AccountNotFound);
        }

        [Fact]
        public async Task GetCreditCardDetailsAsync_WithValidCard_ShouldReturnMappedConsumptions()
        {
            var cardId = 1;
            var clientId = "client-123";

            var card = new CreditCard
            {
                Id = cardId,
                CardNumber = "CARD-123",
                LastFourDigits = "1234",
                CustomerId = clientId,
                CreditLimit = 1000m,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var consumptions = new List<CardConsumption>
            {
                new() { Id = 10, CreditCardId = cardId, Amount = 100, Origin = ConsumptionOrigin.Comercio, CommerceName = "Commerce A", Status = ConsumptionStatus.Aprobado, CreatedAt = DateTimeOffset.UtcNow, CreateByUserId = clientId }
            };

            _creditCardRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<CreditCard, bool>>>(), It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync(card);

            _cardConsumptionRepositoryMock.Setup(r => r.GetAllFindAsync(It.IsAny<Expression<Func<CardConsumption, bool>>>(), It.IsAny<Expression<Func<CardConsumption, object>>[]>()))
                .ReturnsAsync(consumptions);

            var mappedConsumptions = new List<CardConsumptionDto>
            {
                new() { ConsumptionDate = DateTimeOffset.UtcNow, Amount = 100, CommerceName = "Commerce A", StateConsumption = ConsumptionStatus.Aprobado }
            };

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<CardConsumptionDto>>(It.IsAny<List<CardConsumption>>())).Returns(mappedConsumptions);

            var result = await _dashboardService.GetCreditCardDetailsAsync(cardId, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCreditCardDetailsAsync_WithCardNotFoundOrUnowned_ShouldReturnCardNotFoundFailure()
        {
            var cardId = 1;
            var clientId = "client-123";

            _creditCardRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<CreditCard, bool>>>(), It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync((CreditCard)null!);

            var result = await _dashboardService.GetCreditCardDetailsAsync(cardId, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(DashboardError.CardNotFound);
        }

        [Fact]
        public async Task GetLoanDetailsAsync_WithValidLoan_ShouldReturnMappedLoanDetails()
        {
            var loanId = 1;
            var clientId = "client-123";

            var loan = new Loan
            {
                Id = loanId,
                LoanNumber = "LOAN-123",
                CustomerId = clientId,
                ApprovedCapital = 10000m,
                termMonths = TermMonths.Meses12,
                AnnualInterestRate = 0.15m,
                Status = LoanStatus.Activo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            _loansRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<Loan, bool>>>(), It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync(loan);

            var mappedLoan = new DetailLoansDto
            {
                Id = loanId,
                NumberLoand = "LOAN-123",
                CustomerId = clientId,
                FullNameCustomer = "John Doe",
                ApprovedAmount = 10000m,
                AnnualInterestRate = 0.15m,
                Term = 12,
                StateLoans = LoanStatus.Activo,
                loansInstallmentDtos = new List<LoansInstallmentDto>()
            };

            _mapperMock.Setup(m => m.Map<DetailLoansDto>(loan)).Returns(mappedLoan);

            var result = await _dashboardService.GetLoanDetailsAsync(loanId, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Id.Should().Be(loanId);
        }

        [Fact]
        public async Task GetLoanDetailsAsync_WithLoanNotFoundOrUnowned_ShouldReturnLoanNotFoundFailure()
        {
            var loanId = 1;
            var clientId = "client-123";

            _loansRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<Loan, bool>>>(), It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync((Loan)null!);

            var result = await _dashboardService.GetLoanDetailsAsync(loanId, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(DashboardError.LoanNotFound);
        }
    }
}
