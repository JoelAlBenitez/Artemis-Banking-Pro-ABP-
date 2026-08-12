using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.Services.Commerces;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.HermesPay
{
    //Es la operación financiera más delicada de la API: toca deuda, balance, consumo y
    //transacción a la vez, y el rechazo debe dejar todo intacto.
    public sealed class HermesPayServicesTests
    {
        private const string CardNumber = "1589963258467598";
        private const string Cvc = "859";
        private const string CustomerId = "cliente-1";
        private const string CommerceUserId = "usuario-comercio";

        private readonly Mock<ICreditCardsRepository> _creditCardsRepository = new();
        private readonly Mock<ICardConsumptionRepository> _cardConsumptionRepository = new();
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();
        private readonly Mock<ITransactionRepository> _transactionRepository = new();
        private readonly Mock<ICommercePaymentRepository> _commercePaymentRepository = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<IEmailServices> _emailServices = new();
        private readonly Mock<ICvcHasher> _cvcHasher = new();

        private readonly List<CardConsumption> _savedConsumptions = [];
        private readonly List<CommercePayment> _savedPayments = [];
        private readonly List<Transaction> _savedTransactions = [];

        public HermesPayServicesTests()
        {
            _cardConsumptionRepository
                .Setup(repository => repository.AddAsync(It.IsAny<CardConsumption>()))
                .Callback<CardConsumption>(_savedConsumptions.Add)
                .ReturnsAsync((CardConsumption consumption) => consumption);

            _commercePaymentRepository
                .Setup(repository => repository.AddAsync(It.IsAny<CommercePayment>()))
                .Callback<CommercePayment>(_savedPayments.Add)
                .ReturnsAsync((CommercePayment payment) => payment);

            _transactionRepository
                .Setup(repository => repository.AddAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(_savedTransactions.Add)
                .ReturnsAsync((Transaction transaction) => transaction);

            _emailServices
                .Setup(service => service.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(true);

            _userManagementService
                .Setup(service => service.GetUserByIdAsync(CustomerId))
                .ReturnsAsync(BuildCustomer());

            _cvcHasher.Setup(hasher => hasher.Verify(Cvc, It.IsAny<string>())).Returns(true);
        }

        #region Pago aprobado

        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldIncreaseTheDebtAndCreditTheCommerceAccount()
        {
            var card = GivenCard(creditLimit: 50_000m, owedAmount: 10_000m);
            var account = GivenCommerceAccount(balance: 1_000m);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(689.25m));

            result.IsValid.Should().BeTrue();
            card.OwedAmount.Should().Be(10_689.25m);
            card.AvailableCredit.Should().Be(39_310.75m);
            account.Balance.Should().Be(1_689.25m);
        }

        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldRegisterTheConsumptionAgainstTheCommerce()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            var consumption = _savedConsumptions.Single();
            consumption.Status.Should().Be(ConsumptionStatus.Aprobado);
            consumption.Origin.Should().Be(ConsumptionOrigin.Comercio);
            consumption.CommerceId.Should().Be(5);
            consumption.CommerceName.Should().Be("Tienda Demo");
            consumption.Amount.Should().Be(2_500m);
        }

        //La transacción de la cuenta del comercio lleva los últimos cuatro dígitos como origen,
        //nunca el número completo de la tarjeta.
        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldRegisterACreditWithoutExposingTheCardNumber()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            var account = GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            var transaction = _savedTransactions.Single();
            transaction.TransactionType.Should().Be(TransactionType.Credito);
            transaction.OperationType.Should().Be(OperationType.PagoHermesPay);
            transaction.Channel.Should().Be(ChannelPayment.HermesPay);
            transaction.Status.Should().Be(TransactionStatus.Aprobada);
            transaction.Origin.Should().Be("7598");
            transaction.Origin.Should().NotBe(CardNumber);
            transaction.Beneficiary.Should().Be(account.AccountNumber);
        }

        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldLinkThePaymentToTheConsumptionAndTheTransaction()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            var payment = _savedPayments.Single();
            payment.Status.Should().Be(ConsumptionStatus.Aprobado);
            payment.CardLastFourDigits.Should().Be("7598");
            payment.TransactionId.Should().NotBeNull();
        }

        //Un solo SaveChanges: deuda, balance, consumo, transacción y pago se aplican juntos.
        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldPersistEverythingInASingleOperation()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            _commercePaymentRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_WhenApproved_ShouldNotifyTheClientAndTheCommerce()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            _emailServices.Verify(service => service.SendNotification(
                It.Is<MessageDto>(message => message.Subject == "Consumo realizado con la tarjeta 7598")),
                Times.Once);

            _emailServices.Verify(service => service.SendNotification(
                It.Is<MessageDto>(message => message.Subject == "Pago recibido a través de tarjeta 7598")),
                Times.Once);
        }

        //Un fallo de correo no revierte un pago aprobado: se informa como advertencia.
        [Fact]
        public async Task ProcessPayment_WhenTheEmailFails_ShouldKeepThePaymentApproved()
        {
            var card = GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount();

            _emailServices
                .Setup(service => service.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(false);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(2_500m));

            result.Errors.Should().Contain(HermesPayError.PaymentProcessedWithoutNotification);
            card.OwedAmount.Should().Be(2_500m);
            _savedPayments.Single().Status.Should().Be(ConsumptionStatus.Aprobado);
        }

        #endregion

        #region Pago rechazado por crédito

        //Ejemplo del documento: límite 500, deuda 300, disponible 200. Una transacción de 201
        //debe rechazarse.
        [Fact]
        public async Task ProcessPayment_WhenTheAmountExceedsTheAvailableCredit_ShouldRejectIt()
        {
            GivenCard(creditLimit: 500m, owedAmount: 300m);
            GivenCommerceAccount();

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(201m));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(HermesPayError.AmountExceedsAvailableCredit);
        }

        [Fact]
        public async Task ProcessPayment_WhenRejected_ShouldNotChangeBalancesNorDebts()
        {
            var card = GivenCard(creditLimit: 500m, owedAmount: 300m);
            var account = GivenCommerceAccount(balance: 1_000m);

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(201m));

            card.OwedAmount.Should().Be(300m);
            account.Balance.Should().Be(1_000m);
        }

        //El intento rechazado se conserva como evidencia, sin transacción de crédito asociada.
        [Fact]
        public async Task ProcessPayment_WhenRejected_ShouldRecordTheAttemptWithoutATransaction()
        {
            GivenCard(creditLimit: 500m, owedAmount: 300m);
            GivenCommerceAccount();

            await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(201m));

            _savedConsumptions.Single().Status.Should().Be(ConsumptionStatus.Rechazado);
            _savedConsumptions.Single().RejectionReason.Should().Be(RejectionReason.CreditoInsuficiente);

            _savedPayments.Single().Status.Should().Be(ConsumptionStatus.Rechazado);
            _savedPayments.Single().TransactionId.Should().BeNull();

            _savedTransactions.Should().BeEmpty();
        }

        //El límite exacto sí alcanza: se rechaza lo que lo supera, no lo que lo iguala.
        [Fact]
        public async Task ProcessPayment_WithTheExactAvailableCredit_ShouldApproveIt()
        {
            GivenCard(creditLimit: 500m, owedAmount: 300m);
            GivenCommerceAccount();

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(200m));

            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Validaciones de la tarjeta y del comercio

        [Fact]
        public async Task ProcessPayment_WithUnknownCard_ShouldRejectTheRequest()
        {
            GivenCard(card: null);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(100m));

            result.Errors.Should().Contain(HermesPayError.NonExistsCreditCard);
            _savedConsumptions.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessPayment_WithCancelledCard_ShouldRejectTheRequest()
        {
            var card = BuildCard(50_000m, 0m);
            card.Status = CreditCardStatus.Cancelada;
            GivenCard(card);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(100m));

            result.Errors.Should().Contain(HermesPayError.CreditCardIsNotActive);
        }

        [Fact]
        public async Task ProcessPayment_WithExpiredCard_ShouldRejectTheRequest()
        {
            var card = BuildCard(50_000m, 0m);
            card.ExpirationDate = DateTimeOffset.UtcNow.AddDays(-1);
            GivenCard(card);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(100m));

            result.Errors.Should().Contain(HermesPayError.CreditCardExpired);
        }

        //El CVC se compara contra su hash: nunca contra un valor en claro almacenado.
        [Fact]
        public async Task ProcessPayment_WithWrongCvc_ShouldRejectTheRequest()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            _cvcHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(100m));

            result.Errors.Should().Contain(HermesPayError.InvalidCardCredentials);
            _cvcHasher.Verify(hasher => hasher.Verify(Cvc, "hash-del-cvc"), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_WithWrongExpirationDate_ShouldRejectTheRequest()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);

            var payment = BuildPayment(100m);
            payment = new ProcessPaymentDto
            {
                CardNumber = payment.CardNumber,
                MonthExpirationCard = "01",
                YearExpirationCard = "2030",
                Cvc = payment.Cvc,
                TransactionAmount = payment.TransactionAmount
            };

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), payment);

            result.Errors.Should().Contain(HermesPayError.InvalidCardCredentials);
        }

        [Fact]
        public async Task ProcessPayment_WhenTheCommerceHasNoUser_ShouldRejectTheRequest()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);

            var commerce = BuildCommerce();
            commerce.AssociatedUserId = null;

            var result = await BuildService().ProcessPaymentAsync(commerce, BuildPayment(100m));

            result.Errors.Should().Contain(CommerceError.CommerceWithoutAssociatedUser);
        }

        [Fact]
        public async Task ProcessPayment_WhenTheCommerceHasNoActivePrimaryAccount_ShouldRejectTheRequest()
        {
            GivenCard(creditLimit: 50_000m, owedAmount: 0m);
            GivenCommerceAccount(account: null);

            var result = await BuildService().ProcessPaymentAsync(BuildCommerce(), BuildPayment(100m));

            result.Errors.Should().Contain(HermesPayError.CommerceWithoutActivePrimaryAccount);
        }

        #endregion

        #region builders

        private HermesPayServices BuildService()
            => new(_creditCardsRepository.Object,
                   _cardConsumptionRepository.Object,
                   _savingsAccountsRepository.Object,
                   _transactionRepository.Object,
                   _commercePaymentRepository.Object,
                   _userManagementService.Object,
                   _emailServices.Object,
                   _cvcHasher.Object,
                   NullLogger<HermesPayServices>.Instance);

        private CreditCard GivenCard(decimal creditLimit, decimal owedAmount)
            => GivenCard(BuildCard(creditLimit, owedAmount))!;

        private CreditCard? GivenCard(CreditCard? card)
        {
            _creditCardsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync(card);

            return card;
        }

        private SavingsAccount GivenCommerceAccount(decimal balance = 0m)
            => GivenCommerceAccount(BuildCommerceAccount(balance))!;

        private SavingsAccount? GivenCommerceAccount(SavingsAccount? account)
        {
            _savingsAccountsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(account);

            return account;
        }

        private static CreditCard BuildCard(decimal creditLimit, decimal owedAmount)
            => new()
            {
                Id = 1,
                CardNumber = CardNumber,
                LastFourDigits = "7598",
                CustomerId = CustomerId,
                CreditLimit = creditLimit,
                OwedAmount = owedAmount,
                ExpirationDate = new DateTimeOffset(2028, 2, 28, 0, 0, 0, TimeSpan.Zero),
                CvcHash = "hash-del-cvc",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin-1",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        private static SavingsAccount BuildCommerceAccount(decimal balance)
            => new()
            {
                Id = 7,
                AccountNumber = "500000010",
                CustomerId = CommerceUserId,
                Balance = balance,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        private static Commerce BuildCommerce()
            => new()
            {
                Id = 5,
                Name = "Tienda Demo",
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999",
                Status = CommerceStatus.Activo,
                AssociatedUserId = CommerceUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        private static ProcessPaymentDto BuildPayment(decimal amount)
            => new()
            {
                CardNumber = CardNumber,
                MonthExpirationCard = "02",
                YearExpirationCard = "2028",
                Cvc = Cvc,
                TransactionAmount = amount
            };

        private static UserDetailDto BuildCustomer()
            => new()
            {
                Id = CustomerId,
                UserName = "cliente01",
                Name = "Maria",
                LastName = "Gomez",
                IDCARD = "00187654321",
                Email = "cliente01@artemis.com",
                TypeUser = Roles.Cliente,
                State = true
            };

        #endregion
    }
}
