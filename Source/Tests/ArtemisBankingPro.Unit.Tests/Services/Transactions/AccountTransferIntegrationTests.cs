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
    public sealed class AccountTransferIntegrationTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;

        public AccountTransferIntegrationTests()
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
        public async Task ProcessAccountTransferAsync_WhenValid_ShouldDebitSourceCreditDestinationAndCommitAtomically()
        {
            var dbName = $"banking-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            int sourceId, destId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 1000m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Secundaria,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
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
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance,
                    _userManagementServiceMock.Object);

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

                var dto = new AccountTransferDto
                {
                    SourceAccountId = sourceId,
                    DestinationAccountId = destId,
                    Amount = 300m
                };

                _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                    .ReturnsAsync(true);

                _userManagementServiceMock.Setup(u => u.GetUserByIdAsync(clientId))
                    .ReturnsAsync(new UserDetailDto
                    {
                        Id = clientId,
                        UserName = clientId,
                        Name = "Maria",
                        LastName = "Gomez",
                        IDCARD = "001-0000000-1",
                        Email = "maria@artemis.com",
                        TypeUser = Roles.Cliente,
                        State = true,
                        IsClient = true
                    });

                var result = await transactionService.ProcessAccountTransferAsync(dto, clientId);

                result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
                result.Value.Should().NotBeNull();
                result.Value!.Status.Should().Be(TransactionStatus.Aprobada);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                var dbDest = await execContext.SavingsAccounts.FindAsync(destId);
                dbSource!.Balance.Should().Be(700m);
                dbDest!.Balance.Should().Be(800m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(2);

                var debitTx = dbTransactions.First(t => t.TransactionType == TransactionType.Debito);
                var creditTx = dbTransactions.First(t => t.TransactionType == TransactionType.Credito);

                debitTx.Amount.Should().Be(300m);
                debitTx.SavingsAccountId.Should().Be(sourceId);
                debitTx.RelatedTransactionId.Should().BeNull();
                debitTx.Status.Should().Be(TransactionStatus.Aprobada);

                creditTx.Amount.Should().Be(300m);
                creditTx.SavingsAccountId.Should().Be(destId);
                creditTx.RelatedTransactionId.Should().Be(debitTx.Id);
                creditTx.Status.Should().Be(TransactionStatus.Aprobada);
            }
        }

        [Fact]
        public async Task ProcessAccountTransferAsync_WhenInsufficientFunds_ShouldNotChangeBalancesAndPersistRejectedTransaction()
        {
            var dbName = $"banking-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            int sourceId, destId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 100m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                };
                var destAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Secundaria,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
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
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance,
                    _userManagementServiceMock.Object);

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

                var dto = new AccountTransferDto
                {
                    SourceAccountId = sourceId,
                    DestinationAccountId = destId,
                    Amount = 1000m
                };

                var result = await transactionService.ProcessAccountTransferAsync(dto, clientId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(TransactionError.TransferInsufficientFunds);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                var dbDest = await execContext.SavingsAccounts.FindAsync(destId);
                dbSource!.Balance.Should().Be(100m);
                dbDest!.Balance.Should().Be(500m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(1);
                dbTransactions.First().Status.Should().Be(TransactionStatus.Rechazada);
                dbTransactions.First().SavingsAccountId.Should().Be(sourceId);
                dbTransactions.First().Amount.Should().Be(1000m);
                dbTransactions.First().RejectionReason.Should().Be(TransactionError.TransferInsufficientFunds.Description);
            }
        }

        [Fact]
        public async Task ProcessAccountTransferAsync_WhenSameAccount_ShouldNotChangeBalancesAndPersistRejectedTransaction()
        {
            var dbName = $"banking-integration-{Guid.NewGuid()}";
            var clientId = "client-123";

            int sourceId;

            using (var seedContext = CreateContext(dbName))
            {
                var sourceAcc = new SavingsAccount
                {
                    AccountNumber = "100000001",
                    Balance = 500m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                };
                var dummyAcc = new SavingsAccount
                {
                    AccountNumber = "100000002",
                    Balance = 10m,
                    CustomerId = clientId,
                    AccountType = SavingsAccountType.Secundaria,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                };

                await seedContext.SavingsAccounts.AddRangeAsync(sourceAcc, dummyAcc);
                await seedContext.SaveChangesAsync();

                sourceId = sourceAcc.Id;
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
                    savingsRepo, ccRepo, loanRepo, benRepo, instRepo, NullLogger<TransactionsValidationServices>.Instance,
                    _userManagementServiceMock.Object);

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

                var dto = new AccountTransferDto
                {
                    SourceAccountId = sourceId,
                    DestinationAccountId = sourceId,
                    Amount = 100m
                };

                var result = await transactionService.ProcessAccountTransferAsync(dto, clientId);

                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain(TransactionError.TransferSameAccount);

                var dbSource = await execContext.SavingsAccounts.FindAsync(sourceId);
                dbSource!.Balance.Should().Be(500m);

                var dbTransactions = await execContext.Transactions.ToListAsync();
                dbTransactions.Should().HaveCount(1);
                dbTransactions.First().Status.Should().Be(TransactionStatus.Rechazada);
                dbTransactions.First().RejectionReason.Should().Be(TransactionError.TransferSameAccount.Description);
            }
        }
    }
}
