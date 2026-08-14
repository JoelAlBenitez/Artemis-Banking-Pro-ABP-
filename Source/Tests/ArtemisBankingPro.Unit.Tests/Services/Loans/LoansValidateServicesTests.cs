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
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Users;
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
        private const string AdminUserId = "8b1d4f60-1111-4c3a-9d2e-7f6a5b4c3d2e";

        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly LoansValidateServices _validateServices;

        public LoansValidateServicesTests()
        {
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _userManagementServiceMock = new Mock<IUserManagementService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _validateServices = new LoansValidateServices(
                NullLogger<LoansValidateServices>.Instance,
                _loansRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object,
                _userManagementServiceMock.Object,
                _currentUserServiceMock.Object);

            //Cliente existente y activo por defecto: cada prueba que valide lo contrario lo cambia
            GivenCustomer(exists: true, isActive: true);
            GivenAdministratorInSession(AdminUserId);
            GivenCustomerWithoutActiveLoan();
            GivenCustomerWithActivePrimaryAccount();
        }

        private void GivenCustomer(bool exists, bool isActive)
            => _userManagementServiceMock
                .Setup(s => s.ValidateUserExistsByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserExistenceDto { Exists = exists, IsActive = isActive });

        private void GivenAdministratorInSession(string? userId, bool isAdmin = true)
        {
            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock
                .Setup(s => s.IsInRole(Roles.Administrador.ToString()))
                .Returns(isAdmin);
        }

        #region administrador en sesion
        [Fact]
        public void ValidateAdministratorInSession_WithAnAuthenticatedAdministrator_ShouldReturnItsId()
        {
            var result = _validateServices.ValidateAdministratorInSession();

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(AdminUserId);
        }

        [Fact]
        public void ValidateAdministratorInSession_WithoutSession_ShouldFail()
        {
            GivenAdministratorInSession(null);

            var result = _validateServices.ValidateAdministratorInSession();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(LoansError.AdminNotIdentified);
        }

        //Un usuario autenticado que no es administrador tampoco puede firmar la operación.
        [Fact]
        public void ValidateAdministratorInSession_WithoutTheAdministratorRole_ShouldFail()
        {
            GivenAdministratorInSession(AdminUserId, isAdmin: false);

            var result = _validateServices.ValidateAdministratorInSession();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(LoansError.AdminNotIdentified);
        }
        #endregion

        #region cliente en Identity
        [Fact]
        public async Task AssigmentLoansValidateAsync_WithANonExistentCustomer_ShouldFail()
        {
            GivenCustomer(exists: false, isActive: false);

            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.NonExistsCustomerByIdCard);
        }

        //Regla del documento: solo se asignan préstamos a clientes activos.
        [Fact]
        public async Task AssigmentLoansValidateAsync_WithAnInactiveCustomer_ShouldFail()
        {
            GivenCustomer(exists: true, isActive: false);

            var result = await _validateServices.AssigmentLoansValidateAsync(BuildAssignment());

            result.Errors.Should().Contain(LoansError.CustomerIsNotActive);

            //No se llega siquiera a mirar si tiene un préstamo activo
            _loansRepositoryMock.Verify(
                r => r.ExistElementByConsult(It.IsAny<Expression<Func<Loan, bool>>>()), Times.Never);
        }
        #endregion

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
        //Sin cédula el listado va sin filtro de cliente: no se consulta Identity.
        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithoutIdCard_ShouldSucceedWithoutCustomerId()
        {
            var result = await _validateServices.GetLoansByCustomerValidateAsync(new LoansFilterDto());

            result.IsValid.Should().BeTrue();
            result.Value.Should().BeNull();
            _userManagementServiceMock.Verify(
                s => s.GetClientByIdCardAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithoutFilters_ShouldFail()
        {
            var result = await _validateServices.GetLoansByCustomerValidateAsync(null!);

            result.IsValid.Should().BeFalse();
        }

        //La cédula se traduce al Id del cliente: es la clave con la que se filtran los préstamos.
        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithAKnownIdCard_ShouldReturnTheCustomerId()
        {
            _userManagementServiceMock
                .Setup(s => s.GetClientByIdCardAsync("40200000001"))
                .ReturnsAsync(new ClientSummaryDto
                {
                    Id = CustomerId,
                    IDCARD = "40200000001",
                    FullName = "María Gómez",
                    Email = "maria.gomez@artemis.com"
                });

            var result = await _validateServices.GetLoansByCustomerValidateAsync(
                new LoansFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(CustomerId);
        }

        [Fact]
        public async Task GetLoansByCustomerValidateAsync_WithAnUnknownIdCard_ShouldFail()
        {
            _userManagementServiceMock
                .Setup(s => s.GetClientByIdCardAsync(It.IsAny<string>()))
                .ReturnsAsync((ClientSummaryDto?)null);

            var result = await _validateServices.GetLoansByCustomerValidateAsync(
                new LoansFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(LoansError.NonExistsCustomerByIdCard);
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
