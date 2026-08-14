using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CreateSecondaryAccount;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.SavingsAccounts
{
    public sealed class SavingsAccountsHandlersTests
    {
        private const string ClientId = "cliente-1";
        private const string AccountNumber = "500000001";

        private readonly Mock<ISavingsAccountsServices> _savingsAccountsServices = new();
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();

        #region Listado

        [Fact]
        public async Task GetAllSavingsAccounts_ShouldProjectTheDocumentedShape()
        {
            _savingsAccountsServices
                .Setup(service => service.GetPagedSavingsAccountsAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<SavingsAccountDto>>.Success(
                    new PagedResult<SavingsAccountDto>([BuildAccountDto()], 1, 20, 1)));

            var handler = new GetAllSavingsAccountsQueryHandler(
                _savingsAccountsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllSavingsAccountsQuery(), CancellationToken.None);

            var account = result.Data.Single();
            account.AccountNumber.Should().Be(AccountNumber);
            account.ClientId.Should().Be(ClientId);
            account.Identification.Should().Be("00187654321");
            account.Type.Should().Be(nameof(SavingsAccountType.Principal));
            account.Status.Should().Be(nameof(SavingsAccountStatus.Activa));
        }

        //Los tres filtros del documento deben llegar traducidos al contrato del servicio.
        [Fact]
        public async Task GetAllSavingsAccounts_ShouldTranslateTheDocumentedFilters()
        {
            _savingsAccountsServices
                .Setup(service => service.GetPagedSavingsAccountsAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<SavingsAccountDto>>.Success(
                    new PagedResult<SavingsAccountDto>([], 1, 20, 0)));

            var handler = new GetAllSavingsAccountsQueryHandler(
                _savingsAccountsServices.Object, ApiMapperFactory.Create());

            await handler.Handle(new GetAllSavingsAccountsQuery
            {
                Status = "cancelada",
                Type = "secundaria",
                Identification = "00187654321"
            }, CancellationToken.None);

            _savingsAccountsServices.Verify(service => service.GetPagedSavingsAccountsAsync(
                It.Is<SavingsAccountFilterDto>(filter =>
                    filter.Status == SavingsAccountStatusFilter.Canceladas &&
                    filter.Type == SavingsAccountTypeFilter.Secundaria &&
                    filter.IdCard == "00187654321")), Times.Once);
        }

        //El pageSize del documento debe viajar: no queda fijo en el máximo.
        [Fact]
        public async Task GetAllSavingsAccounts_ShouldForwardTheRequestedPageSize()
        {
            _savingsAccountsServices
                .Setup(service => service.GetPagedSavingsAccountsAsync(It.IsAny<SavingsAccountFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<SavingsAccountDto>>.Success(
                    new PagedResult<SavingsAccountDto>([], 2, 5, 0)));

            var handler = new GetAllSavingsAccountsQueryHandler(
                _savingsAccountsServices.Object, ApiMapperFactory.Create());

            await handler.Handle(
                new GetAllSavingsAccountsQuery { Page = 2, PageSize = 5 }, CancellationToken.None);

            _savingsAccountsServices.Verify(service => service.GetPagedSavingsAccountsAsync(
                It.Is<SavingsAccountFilterDto>(filter => filter.Page == 2 && filter.PageSize == 5)), Times.Once);
        }

        #endregion

        #region Transacciones

        //La API identifica la cuenta por su número de 9 dígitos y el servicio por su Id.
        [Fact]
        public async Task GetAccountTransactions_ShouldResolveTheAccountByItsNumber()
        {
            GivenAccountExists(BuildAccount());

            _savingsAccountsServices
                .Setup(service => service.GetPagedTransactionsAsync(1, 1, 20))
                .ReturnsAsync(ValidationResult<PagedResult<TransactionDto>>.Success(
                    new PagedResult<TransactionDto>([BuildTransaction()], 1, 20, 1)));

            _userManagementService
                .Setup(service => service.GetFullNameByIdAsync(ClientId))
                .ReturnsAsync("Maria Gomez");

            var handler = BuildTransactionsHandler();

            var result = await handler.Handle(
                new GetAccountTransactionsQuery { AccountNumber = AccountNumber }, CancellationToken.None);

            result.AccountNumber.Should().Be(AccountNumber);
            result.ClientFullName.Should().Be("Maria Gomez");
            result.Transactions.TotalRecords.Should().Be(1);
        }

        //DÉBITO/CRÉDITO y APROBADA/RECHAZADA viajan escritos como los nombra el documento.
        [Theory]
        [InlineData(TransactionType.Credito, TransactionStatus.Aprobada, "CRÉDITO", "APROBADA")]
        [InlineData(TransactionType.Debito, TransactionStatus.Rechazada, "DÉBITO", "RECHAZADA")]
        public async Task GetAccountTransactions_ShouldWriteTheTypeAndStatusAsTheDocumentDoes(
            TransactionType type, TransactionStatus status, string expectedType, string expectedStatus)
        {
            GivenAccountExists(BuildAccount());

            var transaction = BuildTransaction();
            transaction.TypeTransaction = type;
            transaction.StateTransaction = status;

            _savingsAccountsServices
                .Setup(service => service.GetPagedTransactionsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<PagedResult<TransactionDto>>.Success(
                    new PagedResult<TransactionDto>([transaction], 1, 20, 1)));

            var handler = BuildTransactionsHandler();

            var result = await handler.Handle(
                new GetAccountTransactionsQuery { AccountNumber = AccountNumber }, CancellationToken.None);

            var item = result.Transactions.Data.Single();
            item.TransactionType.Should().Be(expectedType);
            item.Status.Should().Be(expectedStatus);
        }

        [Fact]
        public async Task GetAccountTransactions_WithUnknownAccount_ShouldReportItAsNotFound()
        {
            GivenAccountExists(null);

            var handler = BuildTransactionsHandler();

            var act = async () => await handler.Handle(
                new GetAccountTransactionsQuery { AccountNumber = "999999999" }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("La cuenta seleccionada no existe.");
        }

        #endregion

        #region Asignación

        [Fact]
        public async Task CreateSecondaryAccount_ShouldReturnTheCreatedAccount()
        {
            _savingsAccountsServices
                .Setup(service => service.AssignSavingsAccountAsync(It.IsAny<SavingsAccountAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Success());

            _savingsAccountsRepository
                .Setup(repository => repository.GetAllFindAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync([BuildAccount(id: 3, number: "500000002", type: SavingsAccountType.Secundaria)]);

            _userManagementService
                .Setup(service => service.GetFullNameByIdAsync(ClientId))
                .ReturnsAsync("Maria Gomez");

            var handler = new CreateSecondaryAccountCommandHandler(
                _savingsAccountsServices.Object,
                _savingsAccountsRepository.Object,
                _userManagementService.Object);

            var result = await handler.Handle(
                new CreateSecondaryAccountCommand { ClientId = ClientId, InitialBalance = 5_000m },
                CancellationToken.None);

            result.AccountNumber.Should().Be("500000002");
            result.Type.Should().Be(nameof(SavingsAccountType.Secundaria));
            result.ClientFullName.Should().Be("Maria Gomez");
        }

        //Un cliente sin cuenta principal activa no puede recibir una secundaria.
        [Fact]
        public async Task CreateSecondaryAccount_WithoutActivePrimaryAccount_ShouldRejectTheRequest()
        {
            _savingsAccountsServices
                .Setup(service => service.AssignSavingsAccountAsync(It.IsAny<SavingsAccountAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(
                    SavingsAccountError.CustomerWithoutActivePrimaryAccount));

            var handler = new CreateSecondaryAccountCommandHandler(
                _savingsAccountsServices.Object,
                _savingsAccountsRepository.Object,
                _userManagementService.Object);

            var act = async () => await handler.Handle(
                new CreateSecondaryAccountCommand { ClientId = ClientId, InitialBalance = 0m },
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(SavingsAccountError.CustomerWithoutActivePrimaryAccount.Description);
        }

        //Agotar los reintentos del número de 9 dígitos es un conflicto, no un dato inválido.
        [Fact]
        public async Task CreateSecondaryAccount_WhenTheNumberCannotBeIssued_ShouldReportItAsConflict()
        {
            _savingsAccountsServices
                .Setup(service => service.AssignSavingsAccountAsync(It.IsAny<SavingsAccountAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(SavingsAccountError.FailedGenerateAccountNumber));

            var handler = new CreateSecondaryAccountCommandHandler(
                _savingsAccountsServices.Object,
                _savingsAccountsRepository.Object,
                _userManagementService.Object);

            var act = async () => await handler.Handle(
                new CreateSecondaryAccountCommand { ClientId = ClientId, InitialBalance = 0m },
                CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region Cancelación

        [Fact]
        public async Task CancelSavingsAccount_ShouldCancelTheAccountResolvedByItsNumber()
        {
            GivenAccountExists(BuildAccount(id: 3, number: "500000002", type: SavingsAccountType.Secundaria));

            _savingsAccountsServices
                .Setup(service => service.CancelSavingsAccountAsync(3))
                .ReturnsAsync(ValidationResult.Success());

            var handler = new CancelSavingsAccountCommandHandler(
                _savingsAccountsServices.Object, _savingsAccountsRepository.Object);

            await handler.Handle(
                new CancelSavingsAccountCommand { AccountNumber = "500000002" }, CancellationToken.None);

            _savingsAccountsServices.Verify(service => service.CancelSavingsAccountAsync(3), Times.Once);
        }

        //Mensaje literal del documento para el intento sobre una cuenta principal.
        [Fact]
        public async Task CancelSavingsAccount_OnAPrimaryAccount_ShouldRejectItWithTheDocumentedMessage()
        {
            GivenAccountExists(BuildAccount());

            _savingsAccountsServices
                .Setup(service => service.CancelSavingsAccountAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult.Failure(SavingsAccountError.PrimaryAccountCannotBeCancelled));

            var handler = new CancelSavingsAccountCommandHandler(
                _savingsAccountsServices.Object, _savingsAccountsRepository.Object);

            var act = async () => await handler.Handle(
                new CancelSavingsAccountCommand { AccountNumber = AccountNumber }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Las cuentas principales no pueden ser canceladas.");
        }

        [Fact]
        public async Task CancelSavingsAccount_WithUnknownAccount_ShouldReportItAsNotFound()
        {
            GivenAccountExists(null);

            var handler = new CancelSavingsAccountCommandHandler(
                _savingsAccountsServices.Object, _savingsAccountsRepository.Object);

            var act = async () => await handler.Handle(
                new CancelSavingsAccountCommand { AccountNumber = "999999999" }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
            _savingsAccountsServices.Verify(
                service => service.CancelSavingsAccountAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region builders

        private GetAccountTransactionsQueryHandler BuildTransactionsHandler()
            => new(_savingsAccountsServices.Object,
                   _savingsAccountsRepository.Object,
                   _userManagementService.Object,
                   ApiMapperFactory.Create());

        private void GivenAccountExists(SavingsAccount? account)
            => _savingsAccountsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(account);

        private static SavingsAccount BuildAccount(
            int id = 1,
            string number = AccountNumber,
            SavingsAccountType type = SavingsAccountType.Principal)
            => new()
            {
                Id = id,
                AccountNumber = number,
                CustomerId = ClientId,
                Balance = 17_500m,
                AccountType = type,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        private static SavingsAccountDto BuildAccountDto()
            => new()
            {
                Id = 1,
                AccountNumber = AccountNumber,
                CustomerId = ClientId,
                FullNameCustomer = "Maria Gomez",
                IdCard = "00187654321",
                Balance = 17_500m,
                TypeSavingsAccount = SavingsAccountType.Principal,
                StateSavingsAccount = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow
            };

        private static TransactionDto BuildTransaction()
            => new()
            {
                TransactionDate = DateTimeOffset.UtcNow,
                Amount = 5_000m,
                TypeTransaction = TransactionType.Credito,
                Origin = "DEPÓSITO",
                Beneficiary = AccountNumber,
                StateTransaction = TransactionStatus.Aprobada
            };

        #endregion
    }
}
