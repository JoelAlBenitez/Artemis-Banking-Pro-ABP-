using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.Services.AdminDashboard;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.AdminDashboard
{
    //Indicadores generales del Home del administrador (documento funcional, págs. 17-20).
    public sealed class AdminDashboardServicesTests
    {
        private readonly Mock<ILoansRepository> _loansRepository = new();
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();
        private readonly Mock<ITransactionRepository> _transactionRepository = new();
        private readonly Mock<ICreditCardsRepository> _creditCardsRepository = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<IDebtCalculator> _debtCalculator = new();
        private readonly AdminDashboardServices _sut;

        public AdminDashboardServicesTests()
        {
            _transactionRepository.Setup(r => r.GetTotalHistoricalAsync()).ReturnsAsync(0);
            _transactionRepository.Setup(r => r.GetTotalTodayAsync()).ReturnsAsync(0);
            _transactionRepository
                .Setup(r => r.GetPaymentsAsync(It.IsAny<ChannelPayment?>(), It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(Array.Empty<Transaction>());

            _userManagementService
                .Setup(s => s.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto>());

            _userManagementService
                .Setup(s => s.GetUsersByRoleAsync(It.IsAny<Roles>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(BuildClientsPage(0));

            _loansRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
                .ReturnsAsync(0);

            _creditCardsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<CreditCard, bool>>>()))
                .ReturnsAsync(0);

            _savingsAccountsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(0);

            _debtCalculator
                .Setup(c => c.GetAverageDebtAsync(It.IsAny<IReadOnlyCollection<string>?>()))
                .ReturnsAsync(0m);

            _sut = new AdminDashboardServices(
                NullLogger<AdminDashboardServices>.Instance,
                _loansRepository.Object,
                _savingsAccountsRepository.Object,
                _creditCardsRepository.Object,
                _transactionRepository.Object,
                _userManagementService.Object,
                _debtCalculator.Object);
        }

        private static PagedResponseDto<UserDto> BuildClientsPage(int totalClients)
            => new()
            {
                Items = new List<UserDto>(),
                TotalCount = totalClients,
                Page = 1,
                PageSize = 1
            };

        private static ClientSummaryDto BuildClient(string id)
            => new()
            {
                Id = id,
                IDCARD = $"402000000{id}",
                FullName = $"Cliente {id}",
                Email = $"cliente{id}@artemis.com"
            };

        private static Transaction BuildPayment()
            => new()
            {
                SavingsAccountId = 1,
                Amount = 100m,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.PagoPrestamo,
                Origin = "500000001",
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = "cliente",
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "cliente"
            };

        #region transacciones y pagos
        //Indicadores 1 y 2: el histórico y el del día salen de contadores distintos.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldTakeTransactionsFromTheirOwnCounters()
        {
            _transactionRepository.Setup(r => r.GetTotalHistoricalAsync()).ReturnsAsync(140);
            _transactionRepository.Setup(r => r.GetTotalTodayAsync()).ReturnsAsync(7);

            var result = await _sut.GetDataAdminDashboard();

            result.IsValid.Should().BeTrue();
            result.Value!.TotalHistoricalTransactions.Should().Be(140);
            result.Value.DayTransactions.Should().Be(7);
        }

        //Indicadores 3 y 4: el del día se pide con la fecha actual; el histórico, sin fecha.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldCountHistoricalAndTodayPayments()
        {
            _transactionRepository
                .Setup(r => r.GetPaymentsAsync(null, null))
                .ReturnsAsync(new[] { BuildPayment(), BuildPayment(), BuildPayment() });

            _transactionRepository
                .Setup(r => r.GetPaymentsAsync(null, It.Is<DateTimeOffset?>(d => d != null)))
                .ReturnsAsync(new[] { BuildPayment() });

            var result = await _sut.GetDataAdminDashboard();

            result.Value!.TotalHistoricalPay.Should().Be(3);
            result.Value.DayPay.Should().Be(1);
        }

        //Los pagos del día se piden con la fecha de hoy, no con cualquier fecha.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldAskTodayPaymentsWithTheCurrentDate()
        {
            await _sut.GetDataAdminDashboard();

            _transactionRepository.Verify(
                r => r.GetPaymentsAsync(null, It.Is<DateTimeOffset?>(
                    date => date != null && date.Value.Date == DateTimeOffset.UtcNow.Date)),
                Times.Once);
        }
        #endregion

        #region clientes
        //Indicadores 5 y 6: Identity no expone el conteo de inactivos, se deriva del total.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldDeriveInactiveClientsFromTheTotal()
        {
            _userManagementService
                .Setup(s => s.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto> { BuildClient("1"), BuildClient("2") });

            _userManagementService
                .Setup(s => s.GetUsersByRoleAsync(Roles.Cliente, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(BuildClientsPage(5));

            var result = await _sut.GetDataAdminDashboard();

            result.Value!.CustomerActive.Should().Be(2);
            result.Value.CustomerInactive.Should().Be(3);
        }

        //Un total incoherente con los activos no puede producir un conteo negativo.
        [Fact]
        public async Task GetDataAdminDashboard_WithFewerTotalThanActiveClients_ShouldNotReportNegativeInactives()
        {
            _userManagementService
                .Setup(s => s.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto> { BuildClient("1"), BuildClient("2") });

            _userManagementService
                .Setup(s => s.GetUsersByRoleAsync(Roles.Cliente, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(BuildClientsPage(0));

            var result = await _sut.GetDataAdminDashboard();

            result.Value!.CustomerInactive.Should().Be(0);
        }
        #endregion

        #region productos financieros
        //Indicadores 8, 9, 10 y su suma (7).
        [Fact]
        public async Task GetDataAdminDashboard_ShouldSumTheThreeActiveProductsIntoTheTotal()
        {
            _loansRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
                .ReturnsAsync(4);

            _creditCardsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<CreditCard, bool>>>()))
                .ReturnsAsync(6);

            _savingsAccountsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(11);

            var result = await _sut.GetDataAdminDashboard();

            result.Value!.OutstandingLoans.Should().Be(4);
            result.Value.CreditCardActive.Should().Be(6);
            result.Value.SavingAccountActive.Should().Be(11);
            result.Value.TotalFinancialProducts.Should().Be(21);
        }

        //Los préstamos completados, las tarjetas canceladas y las cuentas canceladas no cuentan.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldOnlyCountActiveProducts()
        {
            Expression<Func<Loan, bool>>? loanFilter = null;
            Expression<Func<CreditCard, bool>>? cardFilter = null;
            Expression<Func<SavingsAccount, bool>>? accountFilter = null;

            _loansRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Loan, bool>>>()))
                .Callback((Expression<Func<Loan, bool>> f) => loanFilter = f)
                .ReturnsAsync(0);

            _creditCardsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<CreditCard, bool>>>()))
                .Callback((Expression<Func<CreditCard, bool>> f) => cardFilter = f)
                .ReturnsAsync(0);

            _savingsAccountsRepository
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .Callback((Expression<Func<SavingsAccount, bool>> f) => accountFilter = f)
                .ReturnsAsync(0);

            await _sut.GetDataAdminDashboard();

            var completedLoan = new Loan
            {
                LoanNumber = "100000001",
                CustomerId = "1",
                ApprovedCapital = 1000m,
                PendingAmount = 0m,
                AnnualInterestRate = 10m,
                termMonths = TermMonths.Meses6,
                Status = LoanStatus.Completado,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var cancelledCard = new CreditCard
            {
                CardNumber = "4111111111111111",
                LastFourDigits = "1111",
                CvcHash = new string('a', 64),
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CustomerId = "1",
                CreditLimit = 5000m,
                OwedAmount = 0m,
                Status = CreditCardStatus.Cancelada,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var cancelledAccount = new SavingsAccount
            {
                AccountNumber = "500000001",
                CustomerId = "1",
                Balance = 0m,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Cancelada,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            loanFilter!.Compile()(completedLoan).Should().BeFalse();
            cardFilter!.Compile()(cancelledCard).Should().BeFalse();
            accountFilter!.Compile()(cancelledAccount).Should().BeFalse();

            //La cuenta principal activa sí entra: el indicador incluye principales y secundarias
            cancelledAccount.Status = SavingsAccountStatus.Activa;
            cancelledAccount.AccountType = SavingsAccountType.Principal;
            accountFilter.Compile()(cancelledAccount).Should().BeTrue();
        }
        #endregion

        #region deuda promedio
        //Indicador 11: el divisor son los clientes activos de Identity, no los que tienen deuda.
        [Fact]
        public async Task GetDataAdminDashboard_ShouldAverageDebtOverTheActiveClientsOfIdentity()
        {
            _userManagementService
                .Setup(s => s.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto> { BuildClient("1"), BuildClient("2") });

            _debtCalculator
                .Setup(c => c.GetAverageDebtAsync(It.IsAny<IReadOnlyCollection<string>?>()))
                .ReturnsAsync(7500.55m);

            var result = await _sut.GetDataAdminDashboard();

            result.Value!.AverageDebtAmountPerCustomer.Should().Be(7500.55m);

            _debtCalculator.Verify(
                c => c.GetAverageDebtAsync(It.Is<IReadOnlyCollection<string>?>(
                    ids => ids != null && ids.Count == 2 && ids.Contains("1") && ids.Contains("2"))),
                Times.Once);
        }

        //Regla explícita: sin clientes activos el promedio se muestra como RD$0.00.
        [Fact]
        public async Task GetDataAdminDashboard_WithoutActiveClients_ShouldReportZeroAverageDebt()
        {
            var result = await _sut.GetDataAdminDashboard();

            result.Value!.CustomerActive.Should().Be(0);
            result.Value.AverageDebtAmountPerCustomer.Should().Be(0m);
        }
        #endregion

        #region fallos
        //El Home no puede tumbarse por un fallo de consulta: devuelve un resultado fallido.
        [Fact]
        public async Task GetDataAdminDashboard_WhenAQueryFails_ShouldFailWithUnexpectedError()
        {
            _transactionRepository
                .Setup(r => r.GetTotalHistoricalAsync())
                .ThrowsAsync(new InvalidOperationException("sin conexión"));

            var result = await _sut.GetDataAdminDashboard();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(GeneralError.UnexpectedError);
        }
        #endregion
    }
}
