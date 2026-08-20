using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Repositories.Loans
{
    //Contrastan el repositorio contra el DbContext real. El proveedor en memoria no soporta
    //secuencias ni índices únicos filtrados: GetNextLoanNumberAsync y la restricción de un solo
    //préstamo activo por cliente solo son verificables contra SQL Server con migraciones (B4).

    // por lo que cuando se realicn las migraciones seran probados con sql server 
    public sealed class LoansRepositoryTests : IDisposable
    {
        private const string CustomerId = "customer-1";

        private readonly DbContextArtemisBanking _context;
        private readonly LoansRepository _repository;

        public LoansRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"loans-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _repository = new LoansRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistTheLoanAndItsInstallmentsWithASingleSaveChanges()
        {
            var loan = BuildLoan(LoanStatus.Activo, DateTimeOffset.UtcNow);
            foreach (var installment in BuildInstallments(12)) loan.loanInstallments.Add(installment);

            await _repository.AddAsync(loan);
            var affected = await _repository.SaveChangesAsync();

            affected.Should().Be(13);
            (await _context.LoanInstallments.CountAsync()).Should().Be(12);
            (await _context.LoanInstallments.AllAsync(i => i.LoanId == loan.Id)).Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedLoansAsync_WithoutStatusFilter_ShouldShowActiveLoansFirst()
        {
            var today = DateTimeOffset.UtcNow;
            await GivenLoans(
                BuildLoan(LoanStatus.Completado, today.AddDays(-1), "100000001"),
                BuildLoan(LoanStatus.Activo, today.AddDays(-10), "100000002"),
                BuildLoan(LoanStatus.Completado, today.AddDays(-30), "100000003"),
                BuildLoan(LoanStatus.Activo, today.AddDays(-2), "100000004"));

            var result = await _repository.GetPagedLoansAsync(1, 20, null, null);

            result.Items.Select(loan => loan.LoanNumber)
                .Should().Equal("100000004", "100000002", "100000001", "100000003");
        }

        [Fact]
        public async Task GetPagedLoansAsync_WithStatusFilter_ShouldOnlyReturnThatStatus()
        {
            var today = DateTimeOffset.UtcNow;
            await GivenLoans(
                BuildLoan(LoanStatus.Activo, today, "100000001"),
                BuildLoan(LoanStatus.Completado, today, "100000002"));

            var result = await _repository.GetPagedLoansAsync(1, 20, LoanStatus.Activo, null);

            result.TotalRecords.Should().Be(1);
            result.Items.Should().OnlyContain(loan => loan.Status == LoanStatus.Activo);
        }

        [Fact]
        public async Task GetPagedLoansAsync_ShouldAlwaysIncludeTheAmortizationTable()
        {
            var loan = BuildLoan(LoanStatus.Activo, DateTimeOffset.UtcNow);
            foreach (var installment in BuildInstallments(6)) loan.loanInstallments.Add(installment);
            await GivenLoans(loan);

            var result = await _repository.GetPagedLoansAsync(1, 20, null, null);

            result.Items.Single().loanInstallments.Should().HaveCount(6);
        }

        [Fact]
        public async Task GetPagedLoansAsync_ShouldNeverExceedTwentyRecordsPerPage()
        {
            var today = DateTimeOffset.UtcNow;
            var loans = Enumerable.Range(1, 25)
                .Select(number => BuildLoan(LoanStatus.Activo, today.AddDays(-number), $"1000000{number:D2}"))
                .ToArray();

            await GivenLoans(loans);

            var firstPage = await _repository.GetPagedLoansAsync(1, 50, null, null);

            firstPage.Items.Should().HaveCount(20);
            firstPage.TotalRecords.Should().Be(25);
            firstPage.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedLoansAsync_WithCustomerFilter_ShouldOnlyReturnTheLoansOfThatCustomer()
        {
            var today = DateTimeOffset.UtcNow;
            var otherCustomerLoan = BuildLoan(LoanStatus.Activo, today, "100000002");
            otherCustomerLoan.CustomerId = "customer-2";

            await GivenLoans(BuildLoan(LoanStatus.Activo, today, "100000001"), otherCustomerLoan);

            var result = await _repository.GetPagedLoansAsync(1, 20, null, CustomerId);

            result.Items.Should().OnlyContain(loan => loan.CustomerId == CustomerId);
        }

        public void Dispose() => _context.Dispose();

        #region helpers
        private async Task GivenLoans(params Loan[] loans)
        {
            await _context.Loans.AddRangeAsync(loans);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private static Loan BuildLoan(LoanStatus status, DateTimeOffset createdAt, string loanNumber = "100000001")
            => new()
            {
                LoanNumber = loanNumber,
                CustomerId = CustomerId,
                ApprovedCapital = 100_000m,
                termMonths = TermMonths.Meses12,
                AnnualInterestRate = 12m,
                MonthlyInstallment = 8_884.88m,
                TotalPayable = 106_618.56m,
                PendingAmount = 106_618.56m,
                Status = status,
                CreatedAt = createdAt,
                CreateByUserId = "admin-1",
                loanInstallments = new List<LoanInstallment>()
            };

        private static IEnumerable<LoanInstallment> BuildInstallments(int total)
            => Enumerable.Range(1, total).Select(number => new LoanInstallment
            {
                LoanId = 0,
                InstallmentNumber = number,
                DueDate = DateTimeOffset.UtcNow.AddMonths(number),
                InstallmentValue = 8_884.88m,
                InterestAmount = 1_000m,
                CapitalAmount = 7_884.88m,
                PendingBalance = 8_884.88m,
                paymentStatus = PaymentStatus.Pendiente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            });
        #endregion
    }
}
