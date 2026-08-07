using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Services.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Loans
{
    public sealed class LoansServicesTests
    {
        private const string CustomerId = "customer-1";
        private const string LoanNumber = "100000001";

        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ILoanInstallmentRepository> _loanInstallmentRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IDebtCalculator> _debtCalculatorMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<ILoansValidateServices> _validateServicesMock;
        private readonly LoansServices _loansServices;

        public LoansServicesTests()
        {
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _loanInstallmentRepositoryMock = new Mock<ILoanInstallmentRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _debtCalculatorMock = new Mock<IDebtCalculator>();
            _emailServicesMock = new Mock<IEmailServices>();
            _validateServicesMock = new Mock<ILoansValidateServices>();

            _loansServices = new LoansServices(
                _loansRepositoryMock.Object,
                _loanInstallmentRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                new AmortizationCalculator(NullLogger<AmortizationCalculator>.Instance),
                _debtCalculatorMock.Object,
                _emailServicesMock.Object,
                BuildMapper(),
                _validateServicesMock.Object,
                NullLogger<LoansServices>.Instance);

            GivenValidAssignment();
            GivenCustomerWithoutDebt();
            GivenActivePrimaryAccount(BuildPrimaryAccount(5_000m));
            GivenGeneratedLoanNumber(LoanNumber);
            GivenPersistenceResult(13);
        }

        #region listado y detalle
        //Regresión del defecto L3: el filtro Todos no debe traducirse a un estado concreto
        [Fact]
        public async Task GetPagedLoansAsync_WithAllFilter_ShouldNotRestrictTheStatus()
        {
            GivenPagedLoans(BuildLoan(LoanStatus.Activo), BuildLoan(LoanStatus.Completado));

            var result = await _loansServices.GetPagedLoansAsync(
                new LoansFilterDto { Status = LoanStatusFilter.Todos });

            result.IsValid.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);

            _loansRepositoryMock.Verify(repository => repository.GetPagedLoansAsync(
                It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<string?>()), Times.Once);
        }

        [Theory]
        [InlineData(LoanStatusFilter.Activos, LoanStatus.Activo)]
        [InlineData(LoanStatusFilter.Completados, LoanStatus.Completado)]
        public async Task GetPagedLoansAsync_WithStatusFilter_ShouldTranslateItToTheLoanStatus(
            LoanStatusFilter filter, LoanStatus expected)
        {
            GivenPagedLoans(BuildLoan(expected));

            await _loansServices.GetPagedLoansAsync(new LoansFilterDto { Status = filter });

            _loansRepositoryMock.Verify(repository => repository.GetPagedLoansAsync(
                It.IsAny<int>(), It.IsAny<int>(), expected, It.IsAny<string?>()), Times.Once);
        }

        //Regresión del defecto L4: el detalle debe traer la tabla de amortización
        [Fact]
        public async Task GetDetailLoanAsync_ShouldLoadTheLoanWithItsInstallments()
        {
            var loan = BuildLoan(LoanStatus.Activo);
            loan.loanInstallments.Add(BuildInstallment(1, DateTimeOffset.UtcNow.AddMonths(1), PaymentStatus.Pendiente));
            GivenLoan(loan);

            var result = await _loansServices.GetDetailLoanAsync(1);

            result.IsValid.Should().BeTrue();
            result.Value!.loansInstallmentDtos.Should().HaveCount(1);

            _loansRepositoryMock.Verify(repository => repository.GetFirstAsync(
                It.IsAny<Expression<Func<Loan, bool>>>(),
                It.Is<Expression<Func<Loan, object>>[]>(includes => includes.Length == 1)), Times.Once);
        }

        [Fact]
        public async Task GetDetailLoanAsync_WithNonExistentLoan_ShouldReturnNonExistsLoan()
        {
            GivenLoan(null);

            var result = await _loansServices.GetDetailLoanAsync(99);

            result.Errors.Should().Contain(LoansError.NonExistsLoan);
        }
        #endregion

        #region evaluacion de riesgo
        [Fact]
        public async Task EvaluateRiskAsync_WhenCurrentDebtExceedsTheAverage_ShouldReportCurrentHighRisk()
        {
            GivenDebt(currentDebt: 90_000m, averageDebt: 80_000m);

            var result = await _loansServices.EvaluateRiskAsync(BuildAssignment());

            result.Value!.RiskType.Should().Be(LoanRiskType.DeudaActual);
            result.Value.Message.Should().Be(LoansError.CustomerWithCurrentHighRisk.Description);
            result.Value.RequiresConfirmation.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateRiskAsync_WhenOnlyTheProjectedDebtExceedsTheAverage_ShouldReportProjectedHighRisk()
        {
            GivenDebt(currentDebt: 25_000m, averageDebt: 80_000m);

            var result = await _loansServices.EvaluateRiskAsync(BuildAssignment());

            result.Value!.RiskType.Should().Be(LoanRiskType.DeudaProyectada);
            result.Value.Message.Should().Be(LoansError.CustomerWithProjectedHighRisk.Description);
            result.Value.ProjectedDebt.Should().BeGreaterThan(result.Value.AverageDebt);
        }

        [Fact]
        public async Task EvaluateRiskAsync_WhenTheProjectedDebtStaysBelowTheAverage_ShouldNotRequireConfirmation()
        {
            GivenDebt(currentDebt: 1_000m, averageDebt: 500_000m);

            var result = await _loansServices.EvaluateRiskAsync(BuildAssignment());

            result.Value!.RiskType.Should().Be(LoanRiskType.SinRiesgo);
            result.Value.RequiresConfirmation.Should().BeFalse();
            result.Value.Message.Should().BeEmpty();
        }
        #endregion

        #region asignacion
        //Regresión del defecto L1: la asignación debe crear el préstamo y su tabla de amortización
        [Fact]
        public async Task CreateAsync_WithValidData_ShouldPersistTheLoanWithItsAmortizationTable()
        {
            Loan? persisted = null;
            _loansRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<Loan>()))
                .Callback<Loan>(loan => persisted = loan)
                .ReturnsAsync((Loan loan) => loan);

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.IsValid.Should().BeTrue();
            persisted.Should().NotBeNull();
            persisted!.LoanNumber.Should().Be(LoanNumber);
            persisted.Status.Should().Be(LoanStatus.Activo);
            persisted.loanInstallments.Should().HaveCount(12);
            persisted.TotalPayable.Should().Be(persisted.loanInstallments.Sum(i => i.InstallmentValue));
            persisted.PendingAmount.Should().Be(persisted.TotalPayable);
            persisted.MonthlyInstallment.Should().Be(persisted.loanInstallments.First().InstallmentValue);
        }

        [Fact]
        public async Task CreateAsync_ShouldDisburseTheApprovedCapitalIntoThePrimaryAccount()
        {
            var primaryAccount = BuildPrimaryAccount(5_000m);
            GivenActivePrimaryAccount(primaryAccount);

            await _loansServices.CreateAsync(BuildAssignment(amount: 100_000m));

            primaryAccount.Balance.Should().Be(105_000m);
            _savingsAccountsRepositoryMock.Verify(repository => repository.UpdateAsync(primaryAccount), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldRegisterTheDisbursementAsACreditTransaction()
        {
            Transaction? registered = null;
            _transactionRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(transaction => registered = transaction)
                .ReturnsAsync((Transaction transaction) => transaction);

            await _loansServices.CreateAsync(BuildAssignment(amount: 100_000m));

            registered.Should().NotBeNull();
            registered!.TransactionType.Should().Be(TransactionType.Credito);
            registered.OperationType.Should().Be(OperationType.DesembolsoPrestamo);
            registered.Amount.Should().Be(100_000m);
            registered.Origin.Should().Be(LoanNumber);
            registered.Status.Should().Be(TransactionStatus.Aprobada);
        }

        [Fact]
        public async Task CreateAsync_ShouldConfirmTheWholeOperationWithASingleSaveChanges()
        {
            await _loansServices.CreateAsync(BuildAssignment());

            _loansRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenTheCustomerIsHighRiskAndTheAdminDidNotConfirm_ShouldNotCreateTheLoan()
        {
            GivenDebt(currentDebt: 90_000m, averageDebt: 80_000m);

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.CustomerWithCurrentHighRisk);
            _loansRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenTheAdminConfirmsTheHighRisk_ShouldCreateTheLoan()
        {
            GivenDebt(currentDebt: 90_000m, averageDebt: 80_000m);

            var result = await _loansServices.CreateAsync(BuildAssignment(confirmHighRisk: true));

            result.IsValid.Should().BeTrue();
            _loansRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenTheValidationFails_ShouldNotTouchThePersistence()
        {
            _validateServicesMock
                .Setup(services => services.AssigmentLoansValidateAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(LoansError.CustomerWithLoanExist));

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.CustomerWithLoanExist);
            _loansRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithoutAnActivePrimaryAccount_ShouldAbortTheAssignment()
        {
            GivenActivePrimaryAccount(null);

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.NonExistAccountFirstActive);
            _loansRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithoutLoanNumber_ShouldReturnFailedGenerateLoanNumber()
        {
            GivenGeneratedLoanNumber(string.Empty);

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.FailedGenerateLoanNumber);
        }

        //Regresión del defecto L7: SaveChangesAsync nunca devuelve negativo
        [Fact]
        public async Task CreateAsync_WhenNothingIsPersisted_ShouldReportTheFailure()
        {
            GivenPersistenceResult(0);

            var result = await _loansServices.CreateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.FailedProcessLoan);
        }
        #endregion

        #region edicion de tasa
        [Fact]
        public async Task EditAnnualInterestRateAsync_WithNegativeRate_ShouldReject()
        {
            var result = await _loansServices.EditAnnualInterestRateAsync(
                new EditAnnualInterestRateDto { Id = 1, AnnualInterestRate = -1m });

            result.Errors.Should().Contain(LoansError.NegativeAnnualInterestRate);
        }

        //Regresión del defecto L5: con las cuotas cargadas la edición de tasa es alcanzable
        [Fact]
        public async Task EditAnnualInterestRateAsync_ShouldRecalculateOnlyTheFuturePendingInstallments()
        {
            var loan = BuildLoan(LoanStatus.Activo);
            var paid = BuildInstallment(1, DateTimeOffset.UtcNow.AddMonths(-1), PaymentStatus.Pagada);
            var future = BuildInstallment(2, DateTimeOffset.UtcNow.AddMonths(1), PaymentStatus.Pendiente);
            loan.loanInstallments.Add(paid);
            loan.loanInstallments.Add(future);

            _validateServicesMock
                .Setup(services => services.EditValidateAnnualInterestRateAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<Loan>.Success(loan));

            var result = await _loansServices.EditAnnualInterestRateAsync(
                new EditAnnualInterestRateDto { Id = 1, AnnualInterestRate = 24m });

            result.IsValid.Should().BeTrue();
            loan.AnnualInterestRate.Should().Be(24m);
            paid.InstallmentValue.Should().Be(8_884.88m);

            _loansRepositoryMock.Verify(repository => repository.UpdateAsync(loan), Times.Once);
            _loanInstallmentRepositoryMock.Verify(
                repository => repository.UpdateRangeLoansInstallmentAsync(
                    It.Is<List<LoanInstallment>>(installments => installments.Count == 1)),
                Times.Once);
        }

        [Fact]
        public async Task EditAnnualInterestRateAsync_WhenTheValidationFails_ShouldNotPersist()
        {
            _validateServicesMock
                .Setup(services => services.EditValidateAnnualInterestRateAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<Loan>.Failure(LoansError.NonExistsFutureInstallments));

            var result = await _loansServices.EditAnnualInterestRateAsync(
                new EditAnnualInterestRateDto { Id = 1, AnnualInterestRate = 24m });

            result.Errors.Should().Contain(LoansError.NonExistsFutureInstallments);
            _loansRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<Loan>()), Times.Never);
        }
        #endregion

        #region helpers
        private static IMapper BuildMapper()
            => new MapperConfiguration(
                configuration =>
                {
                    configuration.AddProfile<LoansMappingEntitieToDtoAndReverse>();
                    configuration.AddProfile<LoansMappingDtoToViewModelAndReverse>();
                },
                NullLoggerFactory.Instance).CreateMapper();

        private void GivenValidAssignment()
        {
            _validateServicesMock
                .Setup(services => services.AssigmentLoansValidateAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Success());

            _validateServicesMock
                .Setup(services => services.GetLoansByCustomerValidateAsync(It.IsAny<LoansFilterDto>()))
                .ReturnsAsync(ValidationResult.Success());
        }

        private void GivenCustomerWithoutDebt() => GivenDebt(0m, 500_000m);

        private void GivenDebt(decimal currentDebt, decimal averageDebt)
        {
            _debtCalculatorMock
                .Setup(calculator => calculator.GetCustomerDebtAsync(It.IsAny<string>()))
                .ReturnsAsync(currentDebt);

            _debtCalculatorMock
                .Setup(calculator => calculator.GetAverageDebtAsync(It.IsAny<IReadOnlyCollection<string>?>()))
                .ReturnsAsync(averageDebt);

            _debtCalculatorMock
                .Setup(calculator => calculator.GetProjectedDebt(It.IsAny<decimal>(), It.IsAny<decimal>()))
                .Returns<decimal, decimal>((current, totalPayable) => current + totalPayable);
        }

        private void GivenActivePrimaryAccount(SavingsAccount? account)
            => _savingsAccountsRepositoryMock
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(account);

        private void GivenGeneratedLoanNumber(string loanNumber)
            => _loansRepositoryMock
                .Setup(repository => repository.GetNextLoanNumberAsync())
                .ReturnsAsync(loanNumber);

        private void GivenPersistenceResult(int result)
            => _loansRepositoryMock
                .Setup(repository => repository.SaveChangesAsync())
                .ReturnsAsync(result);

        private void GivenPagedLoans(params Loan[] loans)
            => _loansRepositoryMock
                .Setup(repository => repository.GetPagedLoansAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<string?>()))
                .ReturnsAsync(new PagedResult<Loan>(loans, 1, 20, loans.Length));

        private void GivenLoan(Loan? loan)
            => _loansRepositoryMock
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync(loan);

        private static LoansAssignmentDto BuildAssignment(
            decimal amount = 100_000m,
            bool confirmHighRisk = false)
            => new()
            {
                CustomerId = CustomerId,
                TermLoans = TermMonths.Meses12,
                AmmountLoans = amount,
                AnnualInterestRate = 12m,
                ConfirmHighRisk = confirmHighRisk
            };

        private static SavingsAccount BuildPrimaryAccount(decimal balance)
            => new()
            {
                Id = 7,
                AccountNumber = "500000001",
                CustomerId = CustomerId,
                Balance = balance,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow.AddYears(-1),
                CreateByUserId = "admin-1"
            };

        private static Loan BuildLoan(LoanStatus status)
            => new()
            {
                Id = 1,
                LoanNumber = LoanNumber,
                CustomerId = CustomerId,
                ApprovedCapital = 100_000m,
                termMonths = TermMonths.Meses12,
                AnnualInterestRate = 12m,
                MonthlyInstallment = 8_884.88m,
                TotalPayable = 106_618.56m,
                PendingAmount = 106_618.56m,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3),
                CreateByUserId = "admin-1",
                loanInstallments = new List<LoanInstallment>()
            };

        private static LoanInstallment BuildInstallment(int number, DateTimeOffset dueDate, PaymentStatus status)
            => new()
            {
                LoanId = 1,
                InstallmentNumber = number,
                DueDate = dueDate,
                InstallmentValue = 8_884.88m,
                InterestAmount = 1_000m,
                CapitalAmount = 7_884.88m,
                PendingBalance = status == PaymentStatus.Pagada ? 0m : 8_884.88m,
                paymentStatus = status,
                CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3),
                CreateByUserId = "admin-1"
            };
        #endregion
    }
}
