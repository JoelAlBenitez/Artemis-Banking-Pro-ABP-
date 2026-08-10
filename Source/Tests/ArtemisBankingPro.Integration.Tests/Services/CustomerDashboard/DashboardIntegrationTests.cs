using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Dashboard;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
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
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Services.CustomerDashboard
{
    public sealed class DashboardIntegrationTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;

        public DashboardIntegrationTests()
        {
            _userManagementServiceMock = new Mock<IUserManagementService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<SavingsAccountsMappingEntitieToDtoAndReverse>();
                cfg.AddProfile<CreditCardMappingProfile>();
                cfg.AddProfile<LoansMappingEntitieToDtoAndReverse>();
                cfg.AddProfile<TransactionMappingProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();
        }

        private DbContextArtemisBanking CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new DbContextArtemisBanking(options);
        }

        [Fact]
        public async Task GetClientDashboardAsync_WhenActiveUser_ShouldReturnDashboardWithRealProfileDetails()
        {
            var dbName = $"dashboard-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            using (var seedContext = CreateContext(dbName))
            {
                var account = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 1500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var card = new CreditCard
                {
                    CardNumber = "1234567890123456",
                    LastFourDigits = "3456",
                    CustomerId = clientId,
                    CreditLimit = 10000m,
                    OwedAmount = 200m,
                    ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                    CvcHash = "hash",
                    Status = CreditCardStatus.Activa,
                    AssignedByAdminId = "admin",
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var loan = new Loan
                {
                    LoanNumber = "LN-0001",
                    ApprovedCapital = 5000m,
                    PendingAmount = 5000m,
                    CustomerId = clientId,
                    AnnualInterestRate = 0.15m,
                    termMonths = TermMonths.Meses12,
                    Status = LoanStatus.Activo,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await seedContext.SavingsAccounts.AddAsync(account);
                await seedContext.CreditCards.AddAsync(card);
                await seedContext.Loans.AddAsync(loan);
                await seedContext.SaveChangesAsync();
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var creditRepo = new CreditCardsRepository(execContext);
                var loansRepo = new LoansRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var cardConsRepo = new CardConsumptionRepository(execContext);

                var dashboardService = new DashboardService(
                    savingsRepo,
                    creditRepo,
                    loansRepo,
                    txRepo,
                    cardConsRepo,
                    _mapper,
                    _userManagementServiceMock.Object
                );

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientId,
                        UserName = "carlos",
                        Name = "Carlos",
                        LastName = "Wilfredo",
                        IDCARD = "001-0000000-4",
                        Email = "carlos@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                var result = await dashboardService.GetClientDashboardAsync(clientId);

                result.IsValid.Should().BeTrue();
                result.Value.Should().NotBeNull();
                result.Value!.ClientName.Should().Be("Carlos Wilfredo");
                result.Value!.ClientEmail.Should().Be("carlos@artemis.com");
                result.Value.SavingsAccounts.Should().HaveCount(1);
                result.Value.CreditCards.Should().HaveCount(1);
                result.Value.Loans.Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task GetClientDashboardAsync_WhenUserInactive_ShouldReturnUnauthorizedAccess()
        {
            var dbName = $"dashboard-integration-{Guid.NewGuid()}";
            var clientId = "inactive-client";

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var creditRepo = new CreditCardsRepository(execContext);
                var loansRepo = new LoansRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var cardConsRepo = new CardConsumptionRepository(execContext);

                var dashboardService = new DashboardService(
                    savingsRepo,
                    creditRepo,
                    loansRepo,
                    txRepo,
                    cardConsRepo,
                    _mapper,
                    _userManagementServiceMock.Object
                );

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = false });

                var result = await dashboardService.GetClientDashboardAsync(clientId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(DashboardError.UnauthorizedAccess);
            }
        }
    }
}
