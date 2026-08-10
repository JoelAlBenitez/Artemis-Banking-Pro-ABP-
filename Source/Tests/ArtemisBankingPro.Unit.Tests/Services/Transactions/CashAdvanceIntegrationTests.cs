using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Transactions;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public sealed class CashAdvanceIntegrationTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;

        public CashAdvanceIntegrationTests()
        {
            _emailServicesMock = new Mock<IEmailServices>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TransactionMappingProfile>();
                cfg.AddProfile<TransactionDtoToViewModelProfile>();
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
        public async Task ProcessCashAdvanceAsync_WhenValid_ShouldChargeInterestOnCardCreditSavingsAndCommitAtomically()
        {
            var dbName = $"cashadvance-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            int cardId, accountId;

            using (var seedContext = CreateContext(dbName))
            {
                var card = new CreditCard
                {
                    CardNumber = "1234567890123456",
                    LastFourDigits = "3456",
                    CreditLimit = 10000m,
                    OwedAmount = 0m,
                    CustomerId = clientId,
                    Status = CreditCardStatus.Activa,
                    ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                    CvcHash = "CvcHashPlaceholder",
                    AssignedByAdminId = "admin-123",
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var account = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await seedContext.CreditCards.AddAsync(card);
                await seedContext.SavingsAccounts.AddAsync(account);
                await seedContext.SaveChangesAsync();

                cardId = card.Id;
                accountId = account.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var ccRepo = new CreditCardsRepository(execContext);
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var cardConsRepo = new CardConsumptionRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var cashAdvRepo = new CashAdvanceRepository(execContext);

                var validationServices = new CashAdvanceValidationServices(
                    ccRepo, savingsRepo, cardConsRepo, NullLogger<CashAdvanceValidationServices>.Instance, _userManagementServiceMock.Object);

                var cashAdvanceService = new CashAdvanceServices(
                    validationServices,
                    cashAdvRepo,
                    ccRepo,
                    savingsRepo,
                    cardConsRepo,
                    txRepo,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<CashAdvanceServices>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new CashAdvanceRequestDto
                {
                    CreditCardId = cardId,
                    SavingsAccountId = accountId,
                    Amount = 1000m
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientId,
                        UserName = clientId,
                        Name = "Carlos",
                        LastName = "Wilfredo",
                        IDCARD = "001-0000000-4",
                        Email = "carlos@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                    .ReturnsAsync(true);

                var result = await cashAdvanceService.ProcessCashAdvanceAsync(dto, clientId);

                result.IsValid.Should().BeTrue();
                result.Value.Should().NotBeNull();
                result.Value!.RequestedAmount.Should().Be(1000m);
                result.Value!.InterestAmount.Should().Be(62.50m);
                result.Value!.TotalCharged.Should().Be(1062.50m);

                var dbCard = await execContext.CreditCards.FindAsync(cardId);
                var dbAccount = await execContext.SavingsAccounts.FindAsync(accountId);
                dbCard!.OwedAmount.Should().Be(1062.50m);
                dbAccount!.Balance.Should().Be(1500m);

                var dbConsumptions = await execContext.CardConsumptions.ToListAsync();
                dbConsumptions.Should().HaveCount(1);
                dbConsumptions.First().Status.Should().Be(ConsumptionStatus.Aprobado);
                dbConsumptions.First().Amount.Should().Be(1062.50m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(1);
                dbTransactions.First().Status.Should().Be(TransactionStatus.Aprobada);
                dbTransactions.First().Amount.Should().Be(1000m);
                dbTransactions.First().SavingsAccountId.Should().Be(accountId);

                var dbAdvances = await execContext.CashAdvances.ToListAsync();
                dbAdvances.Should().HaveCount(1);
                dbAdvances.First().RequestedAmount.Should().Be(1000m);
                dbAdvances.First().InterestAmount.Should().Be(62.50m);
                dbAdvances.First().TotalCharged.Should().Be(1062.50m);
            }
        }

        [Fact]
        public async Task ProcessCashAdvanceAsync_WhenInsufficientCredit_ShouldReturnInsufficientCreditAndPersistRejectedConsumption()
        {
            var dbName = $"cashadvance-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            int cardId, accountId;

            using (var seedContext = CreateContext(dbName))
            {
                var card = new CreditCard
                {
                    CardNumber = "1234567890123456",
                    LastFourDigits = "3456",
                    CreditLimit = 2000m,
                    OwedAmount = 1500m, // Crédito disponible: 500m
                    CustomerId = clientId,
                    Status = CreditCardStatus.Activa,
                    ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                    CvcHash = "CvcHashPlaceholder",
                    AssignedByAdminId = "admin-123",
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var account = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await seedContext.CreditCards.AddAsync(card);
                await seedContext.SavingsAccounts.AddAsync(account);
                await seedContext.SaveChangesAsync();

                cardId = card.Id;
                accountId = account.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var ccRepo = new CreditCardsRepository(execContext);
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var cardConsRepo = new CardConsumptionRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var cashAdvRepo = new CashAdvanceRepository(execContext);

                var validationServices = new CashAdvanceValidationServices(
                    ccRepo, savingsRepo, cardConsRepo, NullLogger<CashAdvanceValidationServices>.Instance, _userManagementServiceMock.Object);

                var cashAdvanceService = new CashAdvanceServices(
                    validationServices,
                    cashAdvRepo,
                    ccRepo,
                    savingsRepo,
                    cardConsRepo,
                    txRepo,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<CashAdvanceServices>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new CashAdvanceRequestDto
                {
                    CreditCardId = cardId,
                    SavingsAccountId = accountId,
                    Amount = 1000m // Total a cargar: 1062.50m (excede los 500m de crédito disponible)
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                var result = await cashAdvanceService.ProcessCashAdvanceAsync(dto, clientId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(CashAdvanceError.InsufficientCredit);

                var dbCard = await execContext.CreditCards.FindAsync(cardId);
                var dbAccount = await execContext.SavingsAccounts.FindAsync(accountId);
                dbCard!.OwedAmount.Should().Be(1500m);
                dbAccount!.Balance.Should().Be(500m);

                var dbConsumptions = await execContext.CardConsumptions.ToListAsync();
                dbConsumptions.Should().HaveCount(1);
                dbConsumptions.First().Status.Should().Be(ConsumptionStatus.Rechazado);
                dbConsumptions.First().RejectionReason.Should().Be(RejectionReason.CreditoInsuficiente);
                dbConsumptions.First().Amount.Should().Be(1062.50m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().BeEmpty();

                var dbAdvances = await execContext.CashAdvances.ToListAsync();
                dbAdvances.Should().BeEmpty();
            }
        }
    }
}
