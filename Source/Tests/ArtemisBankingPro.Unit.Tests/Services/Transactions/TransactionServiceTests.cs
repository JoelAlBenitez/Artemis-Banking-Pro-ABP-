using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

using ArtemisBankingPro.Core.Application.Contracts.Users.Management;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class TransactionServiceTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepositoryMock;
        private readonly Mock<ITransactionsValidationServices> _validationServicesMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TransactionService>> _loggerMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly TransactionService _transactionService;

        //Captura los asientos que el servicio manda a persistir, para poder afirmar sobre el par
        //cruzado sin depender de identificadores que solo existen después de guardar.
        private readonly List<Transaction> _transaccionesRegistradas = new();

        public TransactionServiceTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _beneficiaryRepositoryMock = new Mock<IBeneficiaryRepository>();
            _validationServicesMock = new Mock<ITransactionsValidationServices>();
            _emailServicesMock = new Mock<IEmailServices>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TransactionService>>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            _transactionService = new TransactionService(
                _savingsAccountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _beneficiaryRepositoryMock.Object,
                new Mock<ICreditCardsRepository>().Object,
                new Mock<ILoansRepository>().Object,
                _validationServicesMock.Object,
                _emailServicesMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _userManagementServiceMock.Object);
        }

        [Fact]
        public async Task ProcessExpressAsync_WithValidTransaction_ShouldDecreaseOriginIncreaseDestinationAndReturnSuccess()
        {
            var dto = new ExpressTransactionDto
            {
                SourceAccountNumber = "100000001",
                DestinationAccountNumber = "100000002",
                Amount = 500m
            };
            var clientId = "client-123";

            var originAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = clientId,
                Balance = 1000m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "100000002",
                CustomerId = "client-456",
                Balance = 200m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "client-456"
            };

            _validationServicesMock.Setup(v => v.ValidateExpressAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t)
                .Callback((Transaction t) => _transaccionesRegistradas.Add(t));

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _transactionService.ProcessExpressAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.EffectiveAmount.Should().Be(500m);
            result.Value.Status.Should().Be(TransactionStatus.Aprobada);

            originAccount.Balance.Should().Be(500m);
            destAccount.Balance.Should().Be(700m);

            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(originAccount), Times.Once);
            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(destAccount), Times.Once);

            //Balances y par de asientos viajan en una sola confirmación: si se guardara dos veces,
            //un fallo en la segunda dejaría el dinero movido y la operación reportada como fallida.
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            var registradas = _transaccionesRegistradas;
            registradas.Should().HaveCount(2);

            //El enlace va solo del crédito al débito: el recíproco formaría un ciclo entre dos filas
            //nuevas de la misma tabla y EF abortaría el SaveChangesAsync completo.
            var debito = registradas.Single(t => t.TransactionType == TransactionType.Debito);
            var credito = registradas.Single(t => t.TransactionType == TransactionType.Credito);
            credito.RelatedTransaction.Should().BeSameAs(debito);
            debito.RelatedTransaction.Should().BeNull();
        }

        [Fact]
        public async Task ProcessExpressAsync_WithInsufficientFunds_ShouldRegisterRejectedTransactionAndReturnFailure()
        {
            var dto = new ExpressTransactionDto
            {
                SourceAccountNumber = "100000001",
                DestinationAccountNumber = "100000002",
                Amount = 1500m
            };
            var clientId = "client-123";

            _validationServicesMock.Setup(v => v.ValidateExpressAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds));

            var originAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = clientId,
                Balance = 1000m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "100000002",
                CustomerId = "client-456",
                Balance = 200m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "client-456"
            };

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((Expression<Func<SavingsAccount, bool>> pred, Expression<Func<SavingsAccount, object>>[] includes) =>
                {
                    var func = pred.Compile();
                    if (func(originAccount)) return originAccount;
                    if (func(destAccount)) return destAccount;
                    return null;
                });

            var result = await _transactionService.ProcessExpressAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.InsufficientFunds);

            _transactionRepositoryMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
                t.SavingsAccountId == originAccount.Id &&
                t.Amount == 1500m &&
                t.Status == TransactionStatus.Rechazada &&
                t.RejectionReason == "Fondos insuficientes"
            )), Times.Once);

            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
