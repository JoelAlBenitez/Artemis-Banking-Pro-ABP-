using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Loans
{
    public sealed class LoansValidateServicesTests
    {
        private const string CustomerId = "customer-1";

        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly LoansValidateServices _validateServices;

        public LoansValidateServicesTests()
        {
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();

            _validateServices = new LoansValidateServices(
                NullLogger<LoansValidateServices>.Instance,
                _loansRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object);

            GivenCustomerWithoutActiveLoan();
            GivenCustomerWithActivePrimaryAccount();
        }

        #region asignacion
        [Fact]
        public async Task AssigmentLoansValidateAsync_WithoutData_ShouldFail()
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(null!);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task AssigmentLoansValidateAsync_WithoutSelectedCustomer_ShouldReturnNonSelectedCustomer()
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment(customerId: " "));

            result.Errors.Should().Contain(LoansError.NonSelectedCustomer);
        }

        [Fact]
        public async Task AssigmentLoansValidateAsync_WhenCustomerAlreadyHasAnActiveLoan_ShouldReject()
        {
            _loansRepositoryMock
                .Setup(repository => repository.ExistElementByConsult(It.IsAny<Expression<Func<Loan, bool>>>()))
                .ReturnsAsync(true);

            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.CustomerWithLoanExist);
        }

        //Regresión del defecto L2: el resultado de la validación de plazo se descartaba
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(66)]
        public async Task AssigmentLoansValidateAsync_WithNotAllowedTerm_ShouldReturnInvalidTerm(int term)
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(
                BuildAssignment(term: (TermMonths)term));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(LoansError.InvalidTerm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AssigmentLoansValidateAsync_WithAmountLowerOrEqualToZero_ShouldReturnInvalidAmount(decimal amount)
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment(amount: amount));

            result.Errors.Should().Contain(LoansError.InvalidAmount);
        }

        [Fact]
        public async Task AssigmentLoansValidateAsync_WithNegativeRate_ShouldReturnNegativeAnnualInterestRate()
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment(rate: -0.01m));

            result.Errors.Should().Contain(LoansError.NegativeAnnualInterestRate);
        }

        [Fact]
        public async Task AssigmentLoansValidateAsync_WithoutActivePrimaryAccount_ShouldRejectTheAssignment()
        {
            _savingsAccountsRepositoryMock
                .Setup(repository => repository.ExistElementByConsult(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.NonExistAccountFirstActive);
        }

        [Fact]
        public async Task AssigmentLoansValidateAsync_WithValidData_ShouldSucceed()
        {
            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment());

            result.IsValid.Should().BeTrue();
        }
        #endregion

        #region edicion de tasa
        [Fact]
        public async Task EditValidateAnnualInterestRateAsync_WithNonExistentLoan_ShouldReturnNonExistsLoan()
        {
            GivenLoan(null);

            var result = await _validateServices.EditValidateAnnualInterestRateAsync(1);

            result.Errors.Should().Contain(LoansError.NonExistsLoan);
        }

        [Fact]
        public async Task EditValidateAnnualInterestRateAsync_WithCompletedLoan_ShouldReturnLoanIsNotActive()
        {
            GivenLoan(BuildLoan(LoanStatus.Completado));

            var result = await _validateServices.EditValidateAnnualInterestRateAsync(1);

            result.Errors.Should().Contain(LoansError.LoanIsNotActive);
        }

        [Fact]
        public async Task EditValidateAnnualInterestRateAsync_WithoutFutureInstallments_ShouldReject()
        {
            var loan = BuildLoan(LoanStatus.Activo);
            loan.loanInstallments.Add(BuildInstallment(1, DateTimeOffset.UtcNow.AddMonths(-1), PaymentStatus.Pagada));
            GivenLoan(loan);

            var result = await _validateServices.EditValidateAnnualInterestRateAsync(1);

            result.Errors.Should().Contain(LoansError.NonExistsFutureInstallments);
        }

        //Regresión del defecto L5: el préstamo debe llegar con sus cuotas cargadas
        [Fact]
        public async Task EditValidateAnnualInterestRateAsync_WithFutureInstallments_ShouldReturnTheLoanWithItsInstallments()
        {
            var loan = BuildLoan(LoanStatus.Activo);
            loan.loanInstallments.Add(BuildInstallment(1, DateTimeOffset.UtcNow.AddMonths(1), PaymentStatus.Pendiente));
            GivenLoan(loan);

            var result = await _validateServices.EditValidateAnnualInterestRateAsync(1);

            result.IsValid.Should().BeTrue();
            result.Value!.loanInstallments.Should().HaveCount(1);

            _loansRepositoryMock.Verify(repository => repository.GetFirstAsync(
                It.IsAny<Expression<Func<Loan, bool>>>(),
                It.Is<Expression<Func<Loan, object>>[]>(includes => includes.Length == 1)), Times.Once);
        }
        #endregion

        #region consulta por cedula
        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithoutIdCard_ShouldSucceed()
        {
            var result = await _validateServices.GetLoansByCustomerValidateAsync(new LoansFilterDto());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithoutFilters_ShouldFail()
        {
            var result = await _validateServices.GetLoansByCustomerValidateAsync(null!);

            result.IsValid.Should().BeFalse();
        }
        #endregion

        #region helpers
        private void GivenCustomerWithoutActiveLoan()
            => _loansRepositoryMock
                .Setup(repository => repository.ExistElementByConsult(It.IsAny<Expression<Func<Loan, bool>>>()))
                .ReturnsAsync(false);

        private void GivenCustomerWithActivePrimaryAccount()
            => _savingsAccountsRepositoryMock
                .Setup(repository => repository.ExistElementByConsult(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(true);

        private void GivenLoan(Loan? loan)
            => _loansRepositoryMock
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync(loan);

        private static LoansAssignmentDto BuildAssignment(
            string customerId = CustomerId,
            TermMonths term = TermMonths.Meses12,
            decimal amount = 100_000m,
            decimal rate = 12m)
            => new()
            {
                CustomerId = customerId,
                TermLoans = term,
                AmmountLoans = amount,
                AnnualInterestRate = rate
            };

        private static Loan BuildLoan(LoanStatus status)
            => new()
            {
                Id = 1,
                LoanNumber = "100000001",
                CustomerId = CustomerId,
                ApprovedCapital = 100_000m,
                termMonths = TermMonths.Meses12,
                AnnualInterestRate = 12m,
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
