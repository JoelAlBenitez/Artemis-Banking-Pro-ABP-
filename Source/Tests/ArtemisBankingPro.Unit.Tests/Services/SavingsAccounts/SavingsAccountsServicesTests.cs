using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.SavingsAccounts
{
 
    public sealed class SavingsAccountsServicesTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();
        private readonly Mock<ITransactionRepository> _transactionRepository = new();
        private readonly Mock<ILoansRepository> _loansRepository = new();
        private readonly Mock<ICreditCardsRepository> _creditCardsRepository = new();
        private readonly Mock<ISavingsAccountsValidateServices> _validateServices = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<IEmailServices> _emailServices = new();
        private readonly SavingsAccountsServices _sut;

        private readonly List<Transaction> _registeredTransactions = new();

        private const string CustomerId = "3f2a1c9e-0000-4a2b-9c1d-8e7f6a5b4c3d";
        private const string AdminUserId = "8b1d4f60-1111-4c3a-9d2e-7f6a5b4c3d2e";
        private const string SecondaryAccountNumber = "500000002";
        private const string PrimaryAccountNumber = "500000001";

        public SavingsAccountsServicesTests()
        {
            var mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<SavingsAccountsMappingEntitieToDtoAndReverse>(),
                NullLoggerFactory.Instance).CreateMapper();

            _transactionRepository
                .Setup(repository => repository.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction transaction) =>
                {
                    _registeredTransactions.Add(transaction);
                    return transaction;
                });

            _savingsAccountsRepository
                .Setup(repository => repository.AddAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync((SavingsAccount account) => account);

            _savingsAccountsRepository
                .Setup(repository => repository.UpdateAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync(true);

            _savingsAccountsRepository
                .Setup(repository => repository.SaveChangesAsync())
                .ReturnsAsync(1);

            //Por defecto hay un administrador autenticado: cada prueba que valide lo contrario
            //lo sobrescribe.
            _validateServices
                .Setup(service => service.ValidateAdministratorInSession())
                .Returns(ValidationResult<string>.Success(AdminUserId));

            _sut = new SavingsAccountsServices(
                _savingsAccountsRepository.Object,
                _transactionRepository.Object,
                _loansRepository.Object,
                _creditCardsRepository.Object,
                _validateServices.Object,
                _userManagementService.Object,
                _emailServices.Object,
                mapper,
                NullLogger<SavingsAccountsServices>.Instance);
        }

        private static SavingsAccount BuildAccount(
            int id,
            string accountNumber,
            SavingsAccountType type,
            decimal balance = 0m,
            SavingsAccountStatus status = SavingsAccountStatus.Activa)
            => new()
            {
                Id = id,
                AccountNumber = accountNumber,
                CustomerId = CustomerId,
                Balance = balance,
                AccountType = type,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "SYSTEM"
            };

        private static UserDetailDto BuildCustomer()
            => new()
            {
                Id = CustomerId,
                UserName = "mgomez",
                Name = "María",
                LastName = "Gómez",
                IDCARD = "40200000001",
                Email = "maria.gomez@artemis.com",
                TypeUser = Roles.Cliente,
                State = true,
                IsClient = true
            };

        private void SetupAssignmentIsValid(string generatedAccountNumber = SecondaryAccountNumber)
        {
            _validateServices
                .Setup(service => service.ValidateAssignmentAsync(It.IsAny<SavingsAccountAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Success());

            _savingsAccountsRepository
                .Setup(repository => repository.GetNextAccountNumberAsync())
                .ReturnsAsync(generatedAccountNumber);
        }

        private void SetupCancellationIsValid(SavingsAccount secondary, SavingsAccount? primary)
        {
            _validateServices
                .Setup(service => service.ValidateCancellationAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<SavingsAccount>.Success(secondary));

            _savingsAccountsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(primary);
        }

        #region AssignSavingsAccountAsync
        //Sin administrador en sesión no hay a quién atribuir la cuenta ni sus asientos.
        [Fact]
        public async Task AssignSavingsAccountAsync_WithoutAnAdministratorInSession_ShouldFail()
        {
            _validateServices
                .Setup(service => service.ValidateAdministratorInSession())
                .Returns(ValidationResult<string>.Failure(SavingsAccountError.AdminUserRequired));

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 100m });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.AdminUserRequired);
            _savingsAccountsRepository.Verify(
                repository => repository.SaveChangesAsync(), Times.Never);
        }

        //La cuenta y su asiento inicial se atribuyen al administrador autenticado, no al sistema.
        [Fact]
        public async Task AssignSavingsAccountAsync_ShouldAuditTheAuthenticatedAdministrator()
        {
            SetupAssignmentIsValid();

            SavingsAccount? persisted = null;
            _savingsAccountsRepository
                .Setup(repository => repository.AddAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync((SavingsAccount account) =>
                {
                    persisted = account;
                    return account;
                });

            await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 800m });

            persisted!.CreateByUserId.Should().Be(AdminUserId);

            var entry = _registeredTransactions.Single();
            entry.PerformedByUserId.Should().Be(AdminUserId);
            entry.CreateByUserId.Should().Be(AdminUserId);
        }

        //El correo va después de confirmar: un fallo de envío no revierte la cuenta creada.
        [Fact]
        public async Task AssignSavingsAccountAsync_WhenTheEmailFails_ShouldStillSucceed()
        {
            SetupAssignmentIsValid();

            _userManagementService
                .Setup(service => service.GetUserByIdAsync(CustomerId))
                .ReturnsAsync(BuildCustomer());

            _emailServices
                .Setup(service => service.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(false);

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 100m });

            result.IsValid.Should().BeTrue();
            _emailServices.Verify(
                service => service.SendNotification(
                    It.Is<MessageDto>(message => message.To == "maria.gomez@artemis.com")),
                Times.Once);
        }

        [Fact]
        public async Task AssignSavingsAccountAsync_WithEmptyGeneratedNumber_ShouldFail()
        {
            SetupAssignmentIsValid(string.Empty);

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 100m });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.FailedGenerateAccountNumber);
            _registeredTransactions.Should().BeEmpty();
        }

        [Fact]
        public async Task AssignSavingsAccountAsync_WithZeroBalance_ShouldNotRegisterAnyTransaction()
        {
            SetupAssignmentIsValid();

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 0m });

            result.IsValid.Should().BeTrue();
            _registeredTransactions.Should().BeEmpty();
        }

        //Criterio 5 de la rúbrica: todo balance inicial mayor que cero se asienta como crédito.
        [Fact]
        public async Task AssignSavingsAccountAsync_WithPositiveBalance_ShouldRegisterOneCreditTransaction()
        {
            SetupAssignmentIsValid();

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 2500m });

            result.IsValid.Should().BeTrue();
            _registeredTransactions.Should().ContainSingle();

            var entry = _registeredTransactions.Single();
            entry.Amount.Should().Be(2500m);
            entry.TransactionType.Should().Be(TransactionType.Credito);
            entry.OperationType.Should().Be(OperationType.AperturaCuenta);
            entry.Status.Should().Be(TransactionStatus.Aprobada);
            entry.Origin.Should().Be(SecondaryAccountNumber);
            entry.Beneficiary.Should().Be(SecondaryAccountNumber);
        }

        [Fact]
        public async Task AssignSavingsAccountAsync_ShouldAlwaysCreateASecondaryActiveAccount()
        {
            SetupAssignmentIsValid();

            SavingsAccount? persisted = null;
            _savingsAccountsRepository
                .Setup(repository => repository.AddAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync((SavingsAccount account) =>
                {
                    persisted = account;
                    return account;
                });

            await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 500m });

            persisted.Should().NotBeNull();
            persisted!.AccountType.Should().Be(SavingsAccountType.Secundaria);
            persisted.Status.Should().Be(SavingsAccountStatus.Activa);
            persisted.AccountNumber.Should().Be(SecondaryAccountNumber);
            persisted.Balance.Should().Be(500m);
        }

        [Fact]
        public async Task AssignSavingsAccountAsync_WhenNothingIsPersisted_ShouldFailWithUnexpectedError()
        {
            SetupAssignmentIsValid();

            _savingsAccountsRepository
                .Setup(repository => repository.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 100m });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(GeneralError.UnexpectedError);
        }

        [Fact]
        public async Task AssignSavingsAccountAsync_WhenValidationFails_ShouldNotTouchTheRepository()
        {
            _validateServices
                .Setup(service => service.ValidateAssignmentAsync(It.IsAny<SavingsAccountAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(SavingsAccountError.NegativeInitialBalance));

            var result = await _sut.AssignSavingsAccountAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = -5m });

            result.IsValid.Should().BeFalse();
            _savingsAccountsRepository.Verify(
                repository => repository.SaveChangesAsync(), Times.Never);
        }
        #endregion

        #region CancelSavingsAccountAsync
        [Fact]
        public async Task CancelSavingsAccountAsync_WithoutAnAdministratorInSession_ShouldFail()
        {
            _validateServices
                .Setup(service => service.ValidateAdministratorInSession())
                .Returns(ValidationResult<string>.Failure(SavingsAccountError.AdminUserRequired));

            var result = await _sut.CancelSavingsAccountAsync(2);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.AdminUserRequired);
            _savingsAccountsRepository.Verify(
                repository => repository.SaveChangesAsync(), Times.Never);
        }

        //Ambos asientos y las dos cuentas tocadas quedan a nombre del administrador autenticado.
        [Fact]
        public async Task CancelSavingsAccountAsync_ShouldAuditTheAuthenticatedAdministrator()
        {
            var secondary = BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria, 1200m);
            var primary = BuildAccount(1, PrimaryAccountNumber, SavingsAccountType.Principal, 800m);

            SetupCancellationIsValid(secondary, primary);

            await _sut.CancelSavingsAccountAsync(secondary.Id);

            secondary.LastModifiedByIdUser.Should().Be(AdminUserId);
            primary.LastModifiedByIdUser.Should().Be(AdminUserId);
            _registeredTransactions.Should().OnlyContain(
                t => t.PerformedByUserId == AdminUserId && t.CreateByUserId == AdminUserId);
        }

        //Criterios 8 y 9: el saldo viaja a la principal y quedan dos asientos cruzados.
        [Fact]
        public async Task CancelSavingsAccountAsync_WithBalance_ShouldTransferItAndRegisterTwoTransactions()
        {
            var secondary = BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria, 1200m);
            var primary = BuildAccount(1, PrimaryAccountNumber, SavingsAccountType.Principal, 800m);

            SetupCancellationIsValid(secondary, primary);

            var result = await _sut.CancelSavingsAccountAsync(secondary.Id);

            result.IsValid.Should().BeTrue();
            secondary.Balance.Should().Be(0m);
            primary.Balance.Should().Be(2000m);
            secondary.Status.Should().Be(SavingsAccountStatus.Cancelada);
            secondary.StatusChangedAt.Should().NotBeNull();

            _registeredTransactions.Should().HaveCount(2);

            var debit = _registeredTransactions.Single(t => t.TransactionType == TransactionType.Debito);
            var credit = _registeredTransactions.Single(t => t.TransactionType == TransactionType.Credito);

            debit.SavingsAccountId.Should().Be(secondary.Id);
            credit.SavingsAccountId.Should().Be(primary.Id);
            debit.Amount.Should().Be(1200m);
            credit.Amount.Should().Be(1200m);

            //Origen y beneficiario describen el movimiento, no el lado desde el que se mira
            debit.Origin.Should().Be(SecondaryAccountNumber);
            debit.Beneficiary.Should().Be(PrimaryAccountNumber);
            credit.Origin.Should().Be(SecondaryAccountNumber);
            credit.Beneficiary.Should().Be(PrimaryAccountNumber);

            //El enlace se declara por navegación porque el Id del débito no existe todavía
            credit.RelatedTransaction.Should().BeSameAs(debit);

            _registeredTransactions.Should().OnlyContain(
                t => t.OperationType == OperationType.CancelacionCuenta
                    && t.Status == TransactionStatus.Aprobada);
        }

        [Fact]
        public async Task CancelSavingsAccountAsync_WithoutBalance_ShouldCancelWithoutTransactions()
        {
            var secondary = BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria);
            var primary = BuildAccount(1, PrimaryAccountNumber, SavingsAccountType.Principal, 800m);

            SetupCancellationIsValid(secondary, primary);

            var result = await _sut.CancelSavingsAccountAsync(secondary.Id);

            result.IsValid.Should().BeTrue();
            secondary.Status.Should().Be(SavingsAccountStatus.Cancelada);
            primary.Balance.Should().Be(800m);
            _registeredTransactions.Should().BeEmpty();
        }

        //Atomicidad: balances, estado y asientos se confirman en una sola escritura.
        [Fact]
        public async Task CancelSavingsAccountAsync_ShouldPersistEverythingInASingleSaveChanges()
        {
            var secondary = BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria, 1200m);
            var primary = BuildAccount(1, PrimaryAccountNumber, SavingsAccountType.Principal, 800m);

            SetupCancellationIsValid(secondary, primary);

            await _sut.CancelSavingsAccountAsync(secondary.Id);

            _savingsAccountsRepository.Verify(
                repository => repository.SaveChangesAsync(), Times.Once);
        }

        //La principal puede desaparecer entre la validación y la ejecución: se aborta.
        [Fact]
        public async Task CancelSavingsAccountAsync_WhenThePrimaryAccountDisappears_ShouldFail()
        {
            var secondary = BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria, 1200m);

            SetupCancellationIsValid(secondary, null);

            var result = await _sut.CancelSavingsAccountAsync(secondary.Id);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.WithoutPrimaryAccountToReceiveFunds);
            secondary.Status.Should().Be(SavingsAccountStatus.Activa);
            _savingsAccountsRepository.Verify(
                repository => repository.SaveChangesAsync(), Times.Never);
        }
        #endregion

        #region GetPagedSavingsAccountsAsync
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithACustomerWithoutAccounts_ShouldFail()
        {
            _validateServices
                .Setup(service => service.ValidateCustomerAccountsQueryAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<string?>.Success(CustomerId));

            _savingsAccountsRepository
                .Setup(repository => repository.GetPagedSavingsAccountsAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<SavingsAccountStatus?>(), It.IsAny<SavingsAccountType?>(), It.IsAny<string?>()))
                .ReturnsAsync(new PagedResult<SavingsAccount>(Array.Empty<SavingsAccount>(), 1, 20, 0));

            var result = await _sut.GetPagedSavingsAccountsAsync(
                new SavingsAccountFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsSavingsAccounts);
        }

        //La cédula no existe en Identity: el listado no llega a consultar el repositorio.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_WithAnUnknownIdCard_ShouldFailWithoutQuerying()
        {
            _validateServices
                .Setup(service => service.ValidateCustomerAccountsQueryAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<string?>.Failure(SavingsAccountError.NonExistsCustomerByIdCard));

            var result = await _sut.GetPagedSavingsAccountsAsync(
                new SavingsAccountFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsCustomerByIdCard);
            _savingsAccountsRepository.Verify(
                repository => repository.GetPagedSavingsAccountsAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<SavingsAccountStatus?>(), It.IsAny<SavingsAccountType?>(), It.IsAny<string?>()),
                Times.Never);
        }

        //El nombre y la cédula del titular no están en la entidad: los completa Identity.
        [Fact]
        public async Task GetPagedSavingsAccountsAsync_ShouldFillTheCustomerNameAndIdCardFromIdentity()
        {
            _validateServices
                .Setup(service => service.ValidateCustomerAccountsQueryAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<string?>.Success(null));

            _savingsAccountsRepository
                .Setup(repository => repository.GetPagedSavingsAccountsAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<SavingsAccountStatus?>(), It.IsAny<SavingsAccountType?>(), It.IsAny<string?>()))
                .ReturnsAsync(new PagedResult<SavingsAccount>(
                    new[]
                    {
                        BuildAccount(1, PrimaryAccountNumber, SavingsAccountType.Principal),
                        BuildAccount(2, SecondaryAccountNumber, SavingsAccountType.Secundaria)
                    },
                    1, 20, 2));

            _userManagementService
                .Setup(service => service.GetUserByIdAsync(CustomerId))
                .ReturnsAsync(BuildCustomer());

            var result = await _sut.GetPagedSavingsAccountsAsync(new SavingsAccountFilterDto());

            result.IsValid.Should().BeTrue();
            result.Value!.Items.Should().OnlyContain(
                account => account.FullNameCustomer == "María Gómez" && account.IdCard == "40200000001");

            //Ambas cuentas son del mismo cliente: Identity se consulta una sola vez
            _userManagementService.Verify(
                service => service.GetUserByIdAsync(CustomerId), Times.Once);
        }

        [Theory]
        [InlineData(SavingsAccountStatusFilter.Activas, SavingsAccountStatus.Activa)]
        [InlineData(SavingsAccountStatusFilter.Canceladas, SavingsAccountStatus.Cancelada)]
        [InlineData(SavingsAccountStatusFilter.Todas, null)]
        public async Task GetPagedSavingsAccountsAsync_ShouldTranslateTheStatusFilter(
            SavingsAccountStatusFilter filter, SavingsAccountStatus? expected)
        {
            _validateServices
                .Setup(service => service.ValidateCustomerAccountsQueryAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<string?>.Success(null));

            _savingsAccountsRepository
                .Setup(repository => repository.GetPagedSavingsAccountsAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<SavingsAccountStatus?>(), It.IsAny<SavingsAccountType?>(), It.IsAny<string?>()))
                .ReturnsAsync(new PagedResult<SavingsAccount>(Array.Empty<SavingsAccount>(), 1, 20, 0));

            await _sut.GetPagedSavingsAccountsAsync(new SavingsAccountFilterDto { Status = filter });

            _savingsAccountsRepository.Verify(
                repository => repository.GetPagedSavingsAccountsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), expected, It.IsAny<SavingsAccountType?>(), It.IsAny<string?>()),
                Times.Once);
        }
        #endregion

        #region IsAccountActiveAsync
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task IsAccountActiveAsync_WithoutAccountNumber_ShouldReturnFalseWithoutQuerying(string accountNumber)
        {
            var isActive = await _sut.IsAccountActiveAsync(accountNumber);

            isActive.Should().BeFalse();
            _savingsAccountsRepository.Verify(
                repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>()),
                Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAccountActiveAsync_ShouldReflectTheRepositoryAnswer(bool exists)
        {
            _savingsAccountsRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(exists);

            var isActive = await _sut.IsAccountActiveAsync(SecondaryAccountNumber);

            isActive.Should().Be(exists);
        }

        //El cajero nunca debe operar sobre una verificación que no concluyó.
        [Fact]
        public async Task IsAccountActiveAsync_WhenTheQueryFails_ShouldReturnFalse()
        {
            _savingsAccountsRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("conexión perdida"));

            var isActive = await _sut.IsAccountActiveAsync(SecondaryAccountNumber);

            isActive.Should().BeFalse();
        }
        #endregion

        #region GetCustomerTotalDebtAmountAsync
        [Fact]
        public async Task GetCustomerTotalDebtAmountAsync_ShouldAddActiveLoansAndActiveCards()
        {
            _loansRepository
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, decimal>>>()))
                .ReturnsAsync(15000m);

            _creditCardsRepository
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, decimal>>>()))
                .ReturnsAsync(4500m);

            var debt = await _sut.GetCustomerTotalDebtAmountAsync(CustomerId);

            debt.Should().Be(19500m);
        }

        [Fact]
        public async Task GetCustomerTotalDebtAmountAsync_WithoutCustomer_ShouldReturnZero()
        {
            var debt = await _sut.GetCustomerTotalDebtAmountAsync(string.Empty);

            debt.Should().Be(0m);
            _loansRepository.Verify(
                repository => repository.SumAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, decimal>>>()),
                Times.Never);
        }
        #endregion

        #region GetActiveClientsAsync
        //Criterio 2: paso 1 de la asignación. Los clientes vienen de Identity y la deuda de aquí.
        [Fact]
        public async Task GetActiveClientsAsync_ShouldListActiveClientsWithTheirTotalDebt()
        {
            _userManagementService
                .Setup(service => service.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto>
                {
                    new()
                    {
                        Id = CustomerId,
                        IDCARD = "40200000001",
                        FullName = "María Gómez",
                        Email = "maria.gomez@artemis.com"
                    }
                });

            _loansRepository
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, decimal>>>()))
                .ReturnsAsync(15000m);

            _creditCardsRepository
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, decimal>>>()))
                .ReturnsAsync(4500m);

            var result = await _sut.GetActiveClientsAsync();

            result.IsValid.Should().BeTrue();
            var client = result.Value!.Should().ContainSingle().Subject;
            client.Id.Should().Be(CustomerId);
            client.IdCard.Should().Be("40200000001");
            client.FullName.Should().Be("María Gómez");
            client.Email.Should().Be("maria.gomez@artemis.com");
            client.TotalDebtAmount.Should().Be(19500m);
        }

        [Fact]
        public async Task GetActiveClientsAsync_WithAnIdCard_ShouldNarrowTheListToThatClient()
        {
            _userManagementService
                .Setup(service => service.GetClientByIdCardAsync("40200000001"))
                .ReturnsAsync(new ClientSummaryDto
                {
                    Id = CustomerId,
                    IDCARD = "40200000001",
                    FullName = "María Gómez",
                    Email = "maria.gomez@artemis.com"
                });

            var result = await _sut.GetActiveClientsAsync("40200000001");

            result.IsValid.Should().BeTrue();
            result.Value!.Should().ContainSingle();
            _userManagementService.Verify(
                service => service.GetActiveClientsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetActiveClientsAsync_WithAnUnknownIdCard_ShouldFail()
        {
            _userManagementService
                .Setup(service => service.GetClientByIdCardAsync(It.IsAny<string>()))
                .ReturnsAsync((ClientSummaryDto?)null);

            var result = await _sut.GetActiveClientsAsync("40200000001");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsCustomerByIdCard);
        }
        #endregion
    }
}
