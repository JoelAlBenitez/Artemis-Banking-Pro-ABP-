using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.SavingsAccounts
{
    //Contrastan el repositorio contra el DbContext real. El proveedor en memoria no soporta
    //secuencias ni índices únicos filtrados, así que quedan fuera de estas pruebas:
    //
    //  · GetNextAccountNumberAsync -> SqlQueryRaw sobre SavingsAccountNumberSequence.
    //  · UX_SavingsAccounts_ActivePrimaryPerCustomer -> una sola principal activa por cliente.
    //  · IX único de AccountNumber -> InMemory no lo hace cumplir.
    //
    //Solo son verificables contra SQL Server con migraciones (B4). La construcción del modelo,
    //incluidos esos índices, sí está cubierta por DbContextModelTests.
    public sealed class SavingsAccountsRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-1";
        private const string OtherCustomerId = "customer-2";

        private readonly DbContextArtemisBanking _context;
        private readonly SavingsAccountsRepository _repository;

        public SavingsAccountsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"savings-accounts-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new SavingsAccountsRepository(_context);
        }

        #region orden y filtros del listado
        //Regla propia del módulo: buscando sin estado, las activas van primero y las canceladas
        //después; dentro de cada grupo, de la más reciente a la más antigua.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithoutStatusFilter_ShouldShowActiveAccountsFirst()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Cancelada, createdAt: today.AddDays(-1)),
                BuildAccount("500000002", SavingsAccountStatus.Activa, createdAt: today.AddDays(-10)),
                BuildAccount("500000003", SavingsAccountStatus.Cancelada, createdAt: today.AddDays(-30)),
                BuildAccount("500000004", SavingsAccountStatus.Activa, createdAt: today.AddDays(-2)));

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, null);

            result.Items.Select(account => account.AccountNumber)
                .Should().Equal("500000004", "500000002", "500000001", "500000003");
        }

        //Con filtro de estado el criterio es uno solo: de la más reciente a la más antigua.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithStatusFilter_ShouldOrderFromNewestToOldest()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Activa, createdAt: today.AddDays(-5)),
                BuildAccount("500000002", SavingsAccountStatus.Activa, createdAt: today),
                BuildAccount("500000003", SavingsAccountStatus.Cancelada, createdAt: today),
                BuildAccount("500000004", SavingsAccountStatus.Activa, createdAt: today.AddDays(-1)));

            var result = await _repository.GetPagedSavingsAccountsAsync(
                1, 20, SavingsAccountStatus.Activa, null, null);

            result.TotalRecords.Should().Be(3);
            result.Items.Select(account => account.AccountNumber)
                .Should().Equal("500000002", "500000004", "500000001");
        }

        [Theory]
        [InlineData(SavingsAccountType.Principal, "500000001")]
        [InlineData(SavingsAccountType.Secundaria, "500000002")]
        public async Task GetPagedSavingsAccountsAsync_WithTypeFilter_ShouldOnlyReturnThatType(
            SavingsAccountType type, string expectedAccountNumber)
        {
            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Activa, SavingsAccountType.Principal),
                BuildAccount("500000002", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria));

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, type, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Single().AccountNumber.Should().Be(expectedAccountNumber);
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithCustomerFilter_ShouldOnlyReturnTheAccountsOfThatCustomer()
        {
            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Activa),
                BuildAccount("500000002", SavingsAccountStatus.Activa, customerId: OtherCustomerId),
                BuildAccount("500000003", SavingsAccountStatus.Cancelada, customerId: OtherCustomerId));

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, OtherCustomerId);

            result.TotalRecords.Should().Be(2);
            result.Items.Should().OnlyContain(account => account.CustomerId == OtherCustomerId);
        }

        //Los tres filtros se combinan: el listado admite cédula + estado + tipo a la vez.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_ShouldCombineTheThreeFilters()
        {
            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria),
                BuildAccount("500000002", SavingsAccountStatus.Cancelada, SavingsAccountType.Secundaria),
                BuildAccount("500000003", SavingsAccountStatus.Activa, SavingsAccountType.Principal),
                BuildAccount("500000004", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria,
                    customerId: OtherCustomerId));

            var result = await _repository.GetPagedSavingsAccountsAsync(
                1, 20, SavingsAccountStatus.Activa, SavingsAccountType.Secundaria, CustomerId);

            result.TotalRecords.Should().Be(1);
            result.Items.Single().AccountNumber.Should().Be("500000001");
        }

        //Un cliente sin cuentas devuelve el listado vacío, no una excepción: el mensaje
        //«Este cliente no tiene cuentas de ahorro registradas» lo decide el servicio.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithoutMatches_ShouldReturnAnEmptyPage()
        {
            await GivenAccounts(BuildAccount("500000001", SavingsAccountStatus.Activa));

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, "customer-sin-cuentas");

            result.Items.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }
        #endregion

        #region paginación
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_ShouldNeverExceedTwentyRecordsPerPage()
        {
            await GivenAccounts(BuildManyActiveAccounts(25));

            var firstPage = await _repository.GetPagedSavingsAccountsAsync(1, 50, null, null, null);

            firstPage.Items.Should().HaveCount(20);
            firstPage.PageSize.Should().Be(20);
            firstPage.TotalRecords.Should().Be(25);
            firstPage.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedSavingsAccountsAsync_SecondPage_ShouldContinueWhereTheFirstEnded()
        {
            await GivenAccounts(BuildManyActiveAccounts(25));

            var firstPage = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, null);
            var secondPage = await _repository.GetPagedSavingsAccountsAsync(2, 20, null, null, null);

            secondPage.Items.Should().HaveCount(5);
            secondPage.Page.Should().Be(2);

            //Ninguna cuenta se repite ni se pierde entre páginas
            var everything = firstPage.Items.Concat(secondPage.Items).Select(a => a.AccountNumber).ToList();
            everything.Should().OnlyHaveUniqueItems().And.HaveCount(25);
        }

        //Una página inválida no revienta la consulta: se normaliza a la primera.
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task GetPagedSavingsAccountsAsync_WithAnInvalidPage_ShouldFallBackToTheFirstOne(int page)
        {
            await GivenAccounts(BuildManyActiveAccounts(3));

            var result = await _repository.GetPagedSavingsAccountsAsync(page, 20, null, null, null);

            result.Page.Should().Be(1);
            result.Items.Should().HaveCount(3);
        }

        //El listado es de solo lectura: no arrastra las cuentas al ChangeTracker.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_ShouldNotTrackTheReturnedAccounts()
        {
            await GivenAccounts(BuildAccount("500000001", SavingsAccountStatus.Activa));

            var result = await _repository.GetPagedSavingsAccountsAsync(1, 20, null, null, null);

            result.Items.Should().NotBeEmpty();
            _context.ChangeTracker.Entries<SavingsAccount>().Should().BeEmpty();
        }
        #endregion

        #region asignación y cancelación
        //Criterio 5: la cuenta y su crédito inicial se confirman con un solo SaveChangesAsync.
        //La FK de la transacción la resuelve EF por navegación, sin un Id previo.
        [Fact]
        public async Task AddAsync_ShouldPersistTheAccountAndItsInitialCreditWithASingleSaveChanges()
        {
            var account = BuildAccount("500000001", SavingsAccountStatus.Activa);
            account.Balance = 2500m;

            await _repository.AddAsync(account);

            await _context.Transactions.AddAsync(new Transaction
            {
                SavingsAccount = account,
                SavingsAccountId = default,
                Amount = 2500m,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.AperturaCuenta,
                Origin = "500000001",
                Beneficiary = "500000001",
                Status = TransactionStatus.Aprobada,
                Channel = ChannelPayment.Administrador,
                PerformedByUserId = "admin-1",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            });

            var affected = await _repository.SaveChangesAsync();

            affected.Should().Be(2);

            var persisted = await _context.Transactions.SingleAsync();
            persisted.SavingsAccountId.Should().Be(account.Id);
            persisted.TransactionType.Should().Be(TransactionType.Credito);
        }

        //Criterios 7, 8 y 9: la transferencia a la principal y el cambio de estado se confirman
        //juntos, y la cuenta cancelada conserva su historial.
        [Fact]
        public async Task UpdateAsync_ShouldPersistTheCancellationAndTheTransferInASingleSaveChanges()
        {
            var primary = BuildAccount("500000001", SavingsAccountStatus.Activa, SavingsAccountType.Principal);
            primary.Balance = 800m;

            var secondary = BuildAccount("500000002", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria);
            secondary.Balance = 1200m;

            await GivenAccounts(primary, secondary);

            var toCancel = await _repository.GetByIdAsync(secondary.Id);
            var receiver = await _repository.GetFirstAsync(
                account => account.CustomerId == CustomerId
                    && account.AccountType == SavingsAccountType.Principal
                    && account.Status == SavingsAccountStatus.Activa);

            receiver.Should().NotBeNull();

            var cancelledAt = DateTimeOffset.UtcNow;
            var transferred = toCancel.Balance;

            toCancel.Balance = 0m;
            toCancel.Status = SavingsAccountStatus.Cancelada;
            toCancel.StatusChangedAt = cancelledAt;
            receiver!.Balance += transferred;

            await _repository.UpdateAsync(toCancel);
            await _repository.UpdateAsync(receiver);

            var debit = new Transaction
            {
                SavingsAccountId = toCancel.Id,
                Amount = transferred,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.CancelacionCuenta,
                Origin = toCancel.AccountNumber,
                Beneficiary = receiver.AccountNumber,
                Status = TransactionStatus.Aprobada,
                Channel = ChannelPayment.Administrador,
                PerformedByUserId = "admin-1",
                CreatedAt = cancelledAt,
                CreateByUserId = "admin-1"
            };

            await _context.Transactions.AddAsync(debit);
            await _context.Transactions.AddAsync(new Transaction
            {
                SavingsAccountId = receiver.Id,
                Amount = transferred,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.CancelacionCuenta,
                Origin = toCancel.AccountNumber,
                Beneficiary = receiver.AccountNumber,
                Status = TransactionStatus.Aprobada,
                Channel = ChannelPayment.Administrador,
                PerformedByUserId = "admin-1",
                RelatedTransaction = debit,
                CreatedAt = cancelledAt,
                CreateByUserId = "admin-1"
            });

            await _repository.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var cancelled = await _context.SavingsAccounts.SingleAsync(a => a.AccountNumber == "500000002");
            var primaryAfter = await _context.SavingsAccounts.SingleAsync(a => a.AccountNumber == "500000001");

            cancelled.Status.Should().Be(SavingsAccountStatus.Cancelada);
            cancelled.Balance.Should().Be(0m);
            cancelled.StatusChangedAt.Should().NotBeNull();
            primaryAfter.Balance.Should().Be(2000m);

            //No hay borrado físico: la cancelada sigue en la tabla con su historial
            (await _context.SavingsAccounts.CountAsync()).Should().Be(2);

            var entries = await _context.Transactions.ToListAsync();
            entries.Should().HaveCount(2);
            entries.Single(t => t.TransactionType == TransactionType.Credito)
                .RelatedTransactionId.Should().Be(entries.Single(t => t.TransactionType == TransactionType.Debito).Id);
        }
        #endregion

        #region consultas heredadas que usa el módulo
        //La validación de la principal activa no materializa la fila, solo pregunta si existe.
        [Fact]
        public async Task ExistElementByConsult_ShouldOnlyMatchTheActivePrimaryAccount()
        {
            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Cancelada, SavingsAccountType.Principal),
                BuildAccount("500000002", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria));

            var hasActivePrimary = await _repository.ExistElementByConsult(
                account => account.CustomerId == CustomerId
                    && account.AccountType == SavingsAccountType.Principal
                    && account.Status == SavingsAccountStatus.Activa);

            hasActivePrimary.Should().BeFalse();

            await GivenAccounts(BuildAccount("500000003", SavingsAccountStatus.Activa, SavingsAccountType.Principal));

            (await _repository.ExistElementByConsult(
                account => account.CustomerId == CustomerId
                    && account.AccountType == SavingsAccountType.Principal
                    && account.Status == SavingsAccountStatus.Activa))
                .Should().BeTrue();
        }

        //Respuesta al módulo Cajero: la cuenta existe y está activa.
        [Fact]
        public async Task ExistElementByConsult_ShouldRejectACancelledAccountNumber()
        {
            await GivenAccounts(BuildAccount("500000001", SavingsAccountStatus.Cancelada));

            var isActive = await _repository.ExistElementByConsult(
                account => account.AccountNumber == "500000001"
                    && account.Status == SavingsAccountStatus.Activa);

            isActive.Should().BeFalse();
        }

        //Indicador del Home del administrador: cuenta principales y secundarias activas.
        [Fact]
        public async Task CountAsync_ShouldCountActivePrimaryAndSecondaryAccountsOnly()
        {
            await GivenAccounts(
                BuildAccount("500000001", SavingsAccountStatus.Activa, SavingsAccountType.Principal),
                BuildAccount("500000002", SavingsAccountStatus.Activa, SavingsAccountType.Secundaria),
                BuildAccount("500000003", SavingsAccountStatus.Cancelada, SavingsAccountType.Secundaria));

            var active = await _repository.CountAsync(
                account => account.Status == SavingsAccountStatus.Activa);

            active.Should().Be(2);
        }
        #endregion

        public void Dispose() => _context.Dispose();

        #region helpers
        private async Task GivenAccounts(params SavingsAccount[] accounts)
        {
            await _context.SavingsAccounts.AddRangeAsync(accounts);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static SavingsAccount[] BuildManyActiveAccounts(int total)
            => Enumerable.Range(1, total)
                .Select(number => BuildAccount(
                    $"5000000{number:D2}",
                    SavingsAccountStatus.Activa,
                    createdAt: DateTimeOffset.UtcNow.AddDays(-number)))
                .ToArray();

        private static SavingsAccount BuildAccount(
            string accountNumber,
            SavingsAccountStatus status,
            SavingsAccountType type = SavingsAccountType.Secundaria,
            string customerId = CustomerId,
            DateTimeOffset? createdAt = null)
            => new()
            {
                AccountNumber = accountNumber,
                CustomerId = customerId,
                Balance = 0m,
                AccountType = type,
                Status = status,
                StatusChangedAt = status == SavingsAccountStatus.Cancelada ? DateTimeOffset.UtcNow : null,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };
        #endregion
    }
}
