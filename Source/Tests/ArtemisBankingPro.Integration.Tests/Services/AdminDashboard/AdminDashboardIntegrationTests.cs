using Artemis_Banking_Pro.Core.Application.Services.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.Services.Debts;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Services.AdminDashboard
{
    //Los indicadores contra consultas reales de EF: aquí se ve si los filtros de "activo" y de
    //"fecha de hoy" hacen lo que dice el documento funcional.
    public sealed class AdminDashboardIntegrationTests
    {
        private const string ClientOne = "client-1";
        private const string ClientTwo = "client-2";

        private readonly Mock<IUserManagementService> _userManagementService = new();

        private static DbContextArtemisBanking CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new DbContextArtemisBanking(options);
        }

        private AdminDashboardServices BuildService(DbContextArtemisBanking context)
        {
            var loansRepository = new LoansRepository(context);
            var creditCardsRepository = new CreditCardsRepository(context);

            var debtCalculator = new DebtCalculator(
                loansRepository, creditCardsRepository, NullLogger<DebtCalculator>.Instance);

            return new AdminDashboardServices(
                NullLogger<AdminDashboardServices>.Instance,
                loansRepository,
                new SavingsAccountsRepository(context),
                creditCardsRepository,
                new TransactionRepository(context),
                _userManagementService.Object,
                debtCalculator);
        }

        private void SetupIdentity(int activeClients, int totalClients)
        {
            var clients = new List<ClientSummaryDto>();

            for (var i = 1; i <= activeClients; i++)
            {
                clients.Add(new ClientSummaryDto
                {
                    Id = $"client-{i}",
                    IDCARD = $"4020000000{i}",
                    FullName = $"Cliente {i}",
                    Email = $"cliente{i}@artemis.com"
                });
            }

            _userManagementService.Setup(s => s.GetActiveClientsAsync()).ReturnsAsync(clients);

            _userManagementService
                .Setup(s => s.GetUsersByRoleAsync(Roles.Cliente, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedResponseDto<UserDto>
                {
                    Items = new List<UserDto>(),
                    TotalCount = totalClients,
                    Page = 1,
                    PageSize = 1
                });
        }

        private static SavingsAccount BuildAccount(
            int id, string customerId, SavingsAccountType type, SavingsAccountStatus status)
            => new()
            {
                Id = id,
                AccountNumber = $"50000000{id}",
                CustomerId = customerId,
                Balance = 0m,
                AccountType = type,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

        private static Transaction BuildTransaction(
            int savingsAccountId, OperationType operationType, TransactionStatus status, DateTimeOffset createdAt)
            => new()
            {
                SavingsAccountId = savingsAccountId,
                Amount = 500m,
                TransactionType = TransactionType.Debito,
                OperationType = operationType,
                Origin = "500000001",
                Status = status,
                PerformedByUserId = ClientOne,
                Channel = ChannelPayment.Cliente,
                CreatedAt = createdAt,
                CreateByUserId = ClientOne
            };

        [Fact]
        public async Task GetDataAdminDashboard_ShouldCountOnlyActiveProductsAndTodayOperations()
        {
            var dbName = $"admin-dashboard-{Guid.NewGuid()}";
            var today = DateTimeOffset.UtcNow;
            var yesterday = today.AddDays(-1);

            using (var seed = CreateContext(dbName))
            {
                //2 cuentas activas (una principal y una secundaria) + 1 cancelada
                seed.SavingsAccounts.AddRange(
                    BuildAccount(1, ClientOne, SavingsAccountType.Principal, SavingsAccountStatus.Activa),
                    BuildAccount(2, ClientOne, SavingsAccountType.Secundaria, SavingsAccountStatus.Activa),
                    BuildAccount(3, ClientTwo, SavingsAccountType.Secundaria, SavingsAccountStatus.Cancelada));

                //1 préstamo activo + 1 completado
                seed.Loans.AddRange(
                    new Loan
                    {
                        LoanNumber = "100000001",
                        CustomerId = ClientOne,
                        ApprovedCapital = 10000m,
                        termMonths = TermMonths.Meses12,
                        AnnualInterestRate = 12m,
                        PendingAmount = 6000m,
                        Status = LoanStatus.Activo,
                        CreatedAt = today,
                        CreateByUserId = "admin"
                    },
                    new Loan
                    {
                        LoanNumber = "100000002",
                        CustomerId = ClientTwo,
                        ApprovedCapital = 5000m,
                        termMonths = TermMonths.Meses6,
                        AnnualInterestRate = 10m,
                        PendingAmount = 0m,
                        Status = LoanStatus.Completado,
                        CreatedAt = today,
                        CreateByUserId = "admin"
                    });

                //1 tarjeta activa + 1 cancelada
                seed.CreditCards.AddRange(
                    new CreditCard
                    {
                        CardNumber = "4111111111111111",
                        LastFourDigits = "1111",
                        CustomerId = ClientOne,
                        CreditLimit = 50000m,
                        OwedAmount = 4000m,
                        ExpirationDate = today.AddYears(3),
                        CvcHash = new string('a', 64),
                        Status = CreditCardStatus.Activa,
                        AssignedByAdminId = "admin",
                        CreatedAt = today,
                        CreateByUserId = "admin"
                    },
                    new CreditCard
                    {
                        CardNumber = "4222222222222222",
                        LastFourDigits = "2222",
                        CustomerId = ClientTwo,
                        CreditLimit = 30000m,
                        OwedAmount = 9999m,
                        ExpirationDate = today.AddYears(3),
                        CvcHash = new string('b', 64),
                        Status = CreditCardStatus.Cancelada,
                        AssignedByAdminId = "admin",
                        CreatedAt = today,
                        CreateByUserId = "admin"
                    });

                //4 transacciones: 3 de hoy y 1 de ayer.
                //Pagos aprobados: 1 de hoy (tarjeta) y 1 de ayer (préstamo).
                //El depósito no es pago; el pago rechazado tampoco cuenta.
                seed.Transactions.AddRange(
                    BuildTransaction(1, OperationType.PagoTarjeta, TransactionStatus.Aprobada, today),
                    BuildTransaction(1, OperationType.Deposito, TransactionStatus.Aprobada, today),
                    BuildTransaction(1, OperationType.PagoPrestamo, TransactionStatus.Rechazada, today),
                    BuildTransaction(2, OperationType.PagoPrestamo, TransactionStatus.Aprobada, yesterday));

                await seed.SaveChangesAsync();
            }

            using var context = CreateContext(dbName);

            //3 clientes en total, 2 activos
            SetupIdentity(activeClients: 2, totalClients: 3);

            var result = await BuildService(context).GetDataAdminDashboard();

            result.IsValid.Should().BeTrue();
            var indicators = result.Value!;

            indicators.TotalHistoricalTransactions.Should().Be(4);
            indicators.DayTransactions.Should().Be(3);

            //Pagos = PagoTarjeta + PagoPrestamo aprobados. Depósito y rechazado fuera.
            indicators.TotalHistoricalPay.Should().Be(2);
            indicators.DayPay.Should().Be(1);

            indicators.CustomerActive.Should().Be(2);
            indicators.CustomerInactive.Should().Be(1);

            //Principal y secundaria activas; la cancelada no
            indicators.SavingAccountActive.Should().Be(2);
            indicators.OutstandingLoans.Should().Be(1);
            indicators.CreditCardActive.Should().Be(1);
            indicators.TotalFinancialProducts.Should().Be(4);

            //Deuda de los 2 clientes activos: 6000 del préstamo + 4000 de la tarjeta = 10000.
            //La tarjeta cancelada y el préstamo completado no suman. 10000 / 2 = 5000.
            indicators.AverageDebtAmountPerCustomer.Should().Be(5000m);
        }

        //Regla explícita del documento: sin clientes activos el promedio es RD$0.00.
        [Fact]
        public async Task GetDataAdminDashboard_WithoutActiveClients_ShouldReportZeroAverageDebt()
        {
            var dbName = $"admin-dashboard-empty-{Guid.NewGuid()}";

            using var context = CreateContext(dbName);

            SetupIdentity(activeClients: 0, totalClients: 0);

            var result = await BuildService(context).GetDataAdminDashboard();

            result.IsValid.Should().BeTrue();
            result.Value!.AverageDebtAmountPerCustomer.Should().Be(0m);
            result.Value.TotalFinancialProducts.Should().Be(0);
            result.Value.CustomerActive.Should().Be(0);
            result.Value.CustomerInactive.Should().Be(0);
        }
    }
}
