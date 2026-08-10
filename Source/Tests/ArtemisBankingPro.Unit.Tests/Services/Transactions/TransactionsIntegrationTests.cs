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
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Beneficiaries;
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

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public sealed class TransactionsIntegrationTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;

        public TransactionsIntegrationTests()
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
        public async Task ProcessExpressAsync_WhenValid_ShouldDebitSourceCreditDestinationAndCommitAtomically()
        {
            var dbName = $"transactions-integration-{Guid.NewGuid()}";
            var clientSourceId = "client-source-123";
            var clientDestId = "client-dest-456";

            int sourceId, destId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 2000m,
                    CustomerId = clientSourceId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientSourceId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = clientDestId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientDestId
                };

                await seedContext.SavingsAccounts.AddRangeAsync(sourceAcc, destAcc);
                await seedContext.SaveChangesAsync();

                sourceId = sourceAcc.Id;
                destId = destAcc.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);
                var ccRepo = new CreditCardsRepository(execContext);
                var loanRepo = new LoansRepository(execContext);
                var instRepo = new LoanInstallmentRepository(execContext);

                var validationServices = new TransactionsValidationServices(
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance, _userManagementServiceMock.Object);

                var transactionService = new TransactionService(
                    savingsRepo,
                    txRepo,
                    benRepo,
                    ccRepo,
                    loanRepo,
                    validationServices,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<TransactionService>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new ExpressTransactionDto
                {
                    SourceAccountNumber = "100000001",
                    DestinationAccountNumber = "100000002",
                    Amount = 500m
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientDestId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientSourceId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientSourceId,
                        UserName = clientSourceId,
                        Name = "Juan",
                        LastName = "Perez",
                        IDCARD = "001-0000000-2",
                        Email = "juan@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientDestId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientDestId,
                        UserName = clientDestId,
                        Name = "Ana",
                        LastName = "Mejia",
                        IDCARD = "001-0000000-3",
                        Email = "ana@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                    .ReturnsAsync(true);

                var result = await transactionService.ProcessExpressAsync(dto, clientSourceId);

                result.IsValid.Should().BeTrue();
                result.Value.Should().NotBeNull();
                result.Value!.Status.Should().Be(TransactionStatus.Aprobada);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                var dbDest = await execContext.SavingsAccounts.FindAsync(destId);
                dbSource!.Balance.Should().Be(1500m);
                dbDest!.Balance.Should().Be(800m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(2);

                var debitTx = dbTransactions.First(t => t.TransactionType == TransactionType.Debito);
                var creditTx = dbTransactions.First(t => t.TransactionType == TransactionType.Credito);

                debitTx.Amount.Should().Be(500m);
                debitTx.SavingsAccountId.Should().Be(sourceId);
                debitTx.RelatedTransactionId.Should().Be(creditTx.Id);
                debitTx.Status.Should().Be(TransactionStatus.Aprobada);

                creditTx.Amount.Should().Be(500m);
                creditTx.SavingsAccountId.Should().Be(destId);
                creditTx.RelatedTransactionId.Should().Be(debitTx.Id);
                creditTx.Status.Should().Be(TransactionStatus.Aprobada);
            }
        }

        [Fact]
        public async Task ProcessExpressAsync_WhenInsufficientFunds_ShouldNotChangeBalancesAndPersistRejectedTransaction()
        {
            var dbName = $"transactions-integration-{Guid.NewGuid()}";
            var clientSourceId = "client-source-123";
            var clientDestId = "client-dest-456";

            int sourceId, destId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 100m,
                    CustomerId = clientSourceId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientSourceId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = clientDestId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientDestId
                };

                await seedContext.SavingsAccounts.AddRangeAsync(sourceAcc, destAcc);
                await seedContext.SaveChangesAsync();

                sourceId = sourceAcc.Id;
                destId = destAcc.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);
                var ccRepo = new CreditCardsRepository(execContext);
                var loanRepo = new LoansRepository(execContext);
                var instRepo = new LoanInstallmentRepository(execContext);

                var validationServices = new TransactionsValidationServices(
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance, _userManagementServiceMock.Object);

                var transactionService = new TransactionService(
                    savingsRepo,
                    txRepo,
                    benRepo,
                    ccRepo,
                    loanRepo,
                    validationServices,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<TransactionService>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new ExpressTransactionDto
                {
                    SourceAccountNumber = "100000001",
                    DestinationAccountNumber = "100000002",
                    Amount = 1000m
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientDestId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                var result = await transactionService.ProcessExpressAsync(dto, clientSourceId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(TransactionError.InsufficientFunds);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                var dbDest = await execContext.SavingsAccounts.FindAsync(destId);
                dbSource!.Balance.Should().Be(100m);
                dbDest!.Balance.Should().Be(300m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(1);
                dbTransactions.First().Status.Should().Be(TransactionStatus.Rechazada);
                dbTransactions.First().RejectionReason.Should().Be("Fondos insuficientes");
            }
        }

        [Fact]
        public async Task ProcessExpressAsync_WhenDestinationClientInactive_ShouldReturnDestinationAccountNotFound()
        {
            var dbName = $"transactions-integration-{Guid.NewGuid()}";
            var clientSourceId = "client-source-123";
            var clientDestId = "client-dest-456";

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 1000m,
                    CustomerId = clientSourceId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientSourceId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = clientDestId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientDestId
                };

                await seedContext.SavingsAccounts.AddRangeAsync(sourceAcc, destAcc);
                await seedContext.SaveChangesAsync();
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);
                var ccRepo = new CreditCardsRepository(execContext);
                var loanRepo = new LoansRepository(execContext);
                var instRepo = new LoanInstallmentRepository(execContext);

                var validationServices = new TransactionsValidationServices(
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance, _userManagementServiceMock.Object);

                var transactionService = new TransactionService(
                    savingsRepo,
                    txRepo,
                    benRepo,
                    ccRepo,
                    loanRepo,
                    validationServices,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<TransactionService>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new ExpressTransactionDto
                {
                    SourceAccountNumber = "100000001",
                    DestinationAccountNumber = "100000002",
                    Amount = 200m
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientDestId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = false });

                var result = await transactionService.ProcessExpressAsync(dto, clientSourceId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(TransactionError.DestinationAccountNotFound);
            }
        }

        [Fact]
        public async Task ProcessBeneficiaryTransactionAsync_WhenValid_ShouldDebitSourceCreditDestinationAndCommitAtomically()
        {
            var dbName = $"transactions-integration-{Guid.NewGuid()}";
            var clientSourceId = "client-source-123";
            var clientDestId = "client-dest-456";

            int sourceId, destId, beneficiaryId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 2000m,
                    CustomerId = clientSourceId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientSourceId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 300m,
                    CustomerId = clientDestId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientDestId
                };

                await seedContext.SavingsAccounts.AddRangeAsync(sourceAcc, destAcc);
                await seedContext.SaveChangesAsync();

                sourceId = sourceAcc.Id;
                destId = destAcc.Id;

                var beneficiary = new Beneficiary
                {
                    OwnerClientId = clientSourceId,
                    BeneficiaryAccountNumber = "100000002",
                    BeneficiarySavingsAccountId = destId,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientSourceId
                };

                await seedContext.Beneficiaries.AddAsync(beneficiary);
                await seedContext.SaveChangesAsync();

                beneficiaryId = beneficiary.Id;
            }

            using (var execContext = CreateContext(dbName))
            {
                var savingsRepo = new SavingsAccountsRepository(execContext);
                var txRepo = new TransactionRepository(execContext);
                var benRepo = new BeneficiaryRepository(execContext);
                var ccRepo = new CreditCardsRepository(execContext);
                var loanRepo = new LoansRepository(execContext);
                var instRepo = new LoanInstallmentRepository(execContext);

                var validationServices = new TransactionsValidationServices(
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance, _userManagementServiceMock.Object);

                var transactionService = new TransactionService(
                    savingsRepo,
                    txRepo,
                    benRepo,
                    ccRepo,
                    loanRepo,
                    validationServices,
                    _emailServicesMock.Object,
                    _mapper,
                    NullLogger<TransactionService>.Instance,
                    _userManagementServiceMock.Object);

                var dto = new BeneficiaryTransactionDto
                {
                    BeneficiaryId = beneficiaryId,
                    SourceAccountNumber = "100000001",
                    Amount = 500m
                };

                _userManagementServiceMock.Setup(u => u.ValidateUserExistsByIdAsync(clientDestId))
                    .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientSourceId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientSourceId,
                        UserName = clientSourceId,
                        Name = "Juan",
                        LastName = "Perez",
                        IDCARD = "001-0000000-2",
                        Email = "juan@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientDestId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientDestId,
                        UserName = clientDestId,
                        Name = "Ana",
                        LastName = "Mejia",
                        IDCARD = "001-0000000-3",
                        Email = "ana@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                    .ReturnsAsync(true);

                var result = await transactionService.ProcessBeneficiaryTransactionAsync(dto, clientSourceId);

                result.IsValid.Should().BeTrue();
                result.Value.Should().NotBeNull();
                result.Value!.Status.Should().Be(TransactionStatus.Aprobada);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                var dbDest = await execContext.SavingsAccounts.FindAsync(destId);
                dbSource!.Balance.Should().Be(1500m);
                dbDest!.Balance.Should().Be(800m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(2);
            }
        }
    }
}
