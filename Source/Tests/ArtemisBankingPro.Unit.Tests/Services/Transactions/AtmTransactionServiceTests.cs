using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.Common.Enum;
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

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    //Operaciones del cajero: las cinco mueven dinero real, así que cada prueba comprueba el
    //balance resultante, el tipo de asiento y que la operación quede atribuida al cajero
    //autenticado. Los rechazos se verifican por lo que NO cambian.
    public class AtmTransactionServiceTests
    {
        private const string CashierId = "cajero-001";
        private const string CustomerId = "cliente-001";

        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock = new();
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();
        private readonly Mock<ICreditCardsRepository> _creditCardRepositoryMock = new();
        private readonly Mock<IEmailServices> _emailServicesMock = new();

        private readonly List<Transaction> _asientos = new();
        private readonly IAtmTransactionService _service;

        public AtmTransactionServiceTests()
        {
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t)
                .Callback((Transaction t) => _asientos.Add(t));

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _savingsAccountRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SavingsAccount>())).ReturnsAsync(true);
            _creditCardRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CreditCard>())).ReturnsAsync(true);

            _service = new TransactionService(
                _savingsAccountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                new Mock<IBeneficiaryRepository>().Object,
                _creditCardRepositoryMock.Object,
                new Mock<ILoansRepository>().Object,
                new Mock<ITransactionsValidationServices>().Object,
                _emailServicesMock.Object,
                new Mock<IMapper>().Object,
                new Mock<ILogger<TransactionService>>().Object,
                new Mock<IUserManagementService>().Object);
        }

        #region helpers

        private static SavingsAccount Cuenta(int id, string numero, decimal balance,
            SavingsAccountStatus estado = SavingsAccountStatus.Activa, string? cliente = null)
            => new()
            {
                Id = id,
                AccountNumber = numero,
                CustomerId = cliente ?? CustomerId,
                Balance = balance,
                AccountType = SavingsAccountType.Principal,
                Status = estado,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

        private static CreditCard Tarjeta(string numero, decimal deuda,
            CreditCardStatus estado = CreditCardStatus.Activa)
            => new()
            {
                Id = 1,
                CardNumber = numero,
                LastFourDigits = numero[^4..],
                CustomerId = CustomerId,
                CreditLimit = 50_000m,
                OwedAmount = deuda,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CvcHash = new string('a', 64),
                Status = estado,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

        private void RegistrarCuentas(params SavingsAccount[] cuentas)
        {
            foreach (var cuenta in cuentas)
            {
                var numero = cuenta.AccountNumber;
                _savingsAccountRepositoryMock
                    .Setup(r => r.GetFirstAsync(
                        It.Is<Expression<Func<SavingsAccount, bool>>>(p => p.Compile()(cuenta)),
                        It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                    .ReturnsAsync(cuenta);
            }
        }

        private void SinCuenta()
            => _savingsAccountRepositoryMock
                .Setup(r => r.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((SavingsAccount?)null);

        private void RegistrarTarjeta(CreditCard tarjeta)
            => _creditCardRepositoryMock
                .Setup(r => r.GetFirstAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync(tarjeta);

        #endregion

        #region depósito

        [Fact]
        public async Task ProcessAtmDepositAsync_ConCuentaActiva_AcreditaElBalanceYRegistraCredito()
        {
            var cuenta = Cuenta(1, "500000001", 1_000m);
            RegistrarCuentas(cuenta);

            var resultado = await _service.ProcessAtmDepositAsync(new AtmDepositDto
            {
                DestinationAccountNumber = "500000001",
                Amount = 2_500m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeTrue();
            cuenta.Balance.Should().Be(3_500m);

            var asiento = _asientos.Should().ContainSingle().Subject;
            asiento.TransactionType.Should().Be(TransactionType.Credito);
            asiento.OperationType.Should().Be(OperationType.Deposito);
            asiento.Status.Should().Be(TransactionStatus.Aprobada);
            asiento.Amount.Should().Be(2_500m);

            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessAtmDepositAsync_AtribuyeLaOperacionAlCajeroAutenticado()
        {
            var cuenta = Cuenta(1, "500000001", 0m);
            RegistrarCuentas(cuenta);

            await _service.ProcessAtmDepositAsync(new AtmDepositDto
            {
                DestinationAccountNumber = "500000001",
                Amount = 100m,
                CashierId = CashierId
            });

            var asiento = _asientos.Should().ContainSingle().Subject;
            asiento.PerformedByUserId.Should().Be(CashierId);
            asiento.CreateByUserId.Should().Be(CashierId);
            asiento.Channel.Should().Be(ChannelPayment.Cajero);
        }

        [Fact]
        public async Task ProcessAtmDepositAsync_ConCuentaCancelada_RechazaSinTocarElBalance()
        {
            var cuenta = Cuenta(1, "500000001", 1_000m, SavingsAccountStatus.Cancelada);
            RegistrarCuentas(cuenta);

            var resultado = await _service.ProcessAtmDepositAsync(new AtmDepositDto
            {
                DestinationAccountNumber = "500000001",
                Amount = 500m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            cuenta.Balance.Should().Be(1_000m);
            _asientos.Should().BeEmpty();
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ProcessAtmDepositAsync_ConCuentaInexistente_DevuelveFallo()
        {
            SinCuenta();

            var resultado = await _service.ProcessAtmDepositAsync(new AtmDepositDto
            {
                DestinationAccountNumber = "999999999",
                Amount = 500m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            _asientos.Should().BeEmpty();
        }

        #endregion

        #region retiro

        [Fact]
        public async Task ProcessAtmWithdrawalAsync_ConFondosSuficientes_DebitaYRegistraDebito()
        {
            var cuenta = Cuenta(1, "500000001", 1_000m);
            RegistrarCuentas(cuenta);

            var resultado = await _service.ProcessAtmWithdrawalAsync(new AtmWithdrawalDto
            {
                SourceAccountNumber = "500000001",
                Amount = 400m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeTrue();
            cuenta.Balance.Should().Be(600m);

            var asiento = _asientos.Should().ContainSingle().Subject;
            asiento.TransactionType.Should().Be(TransactionType.Debito);
            asiento.OperationType.Should().Be(OperationType.Retiro);
            asiento.Status.Should().Be(TransactionStatus.Aprobada);
            asiento.PerformedByUserId.Should().Be(CashierId);
        }

        [Fact]
        public async Task ProcessAtmWithdrawalAsync_SinFondos_RegistraElIntentoRechazadoYNoMueveElBalance()
        {
            var cuenta = Cuenta(1, "500000001", 100m);
            RegistrarCuentas(cuenta);

            var resultado = await _service.ProcessAtmWithdrawalAsync(new AtmWithdrawalDto
            {
                SourceAccountNumber = "500000001",
                Amount = 5_000m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            cuenta.Balance.Should().Be(100m);

            var rechazado = _asientos.Should().ContainSingle().Subject;
            rechazado.Status.Should().Be(TransactionStatus.Rechazada);
            rechazado.RejectionReason.Should().Be("Fondos insuficientes");
            rechazado.TransactionType.Should().Be(TransactionType.Debito);

            //El rechazo también se persiste: queda en el historial sin afectar balances.
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SavingsAccount>()), Times.Never);
        }

        #endregion

        #region pago de tarjeta

        [Fact]
        public async Task ProcessAtmCreditCardPaymentAsync_ConDeudaYFondos_DebitaLaCuentaYReduceLaDeuda()
        {
            var cuenta = Cuenta(1, "500000001", 5_000m);
            RegistrarCuentas(cuenta);
            var tarjeta = Tarjeta("4111111111111111", 1_200m);
            RegistrarTarjeta(tarjeta);

            var resultado = await _service.ProcessAtmCreditCardPaymentAsync(new AtmCreditCardPaymentDto
            {
                SourceAccountNumber = "500000001",
                CreditCardNumber = "4111111111111111",
                Amount = 500m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeTrue();
            cuenta.Balance.Should().Be(4_500m);
            tarjeta.OwedAmount.Should().Be(700m);

            var asiento = _asientos.Should().ContainSingle().Subject;
            asiento.TransactionType.Should().Be(TransactionType.Debito);
            asiento.OperationType.Should().Be(OperationType.PagoTarjeta);
            asiento.Amount.Should().Be(500m);
        }

        [Fact]
        public async Task ProcessAtmCreditCardPaymentAsync_ConMontoMayorALaDeuda_NoPermiteSobrepago()
        {
            var cuenta = Cuenta(1, "500000001", 10_000m);
            RegistrarCuentas(cuenta);
            var tarjeta = Tarjeta("4111111111111111", 300m);
            RegistrarTarjeta(tarjeta);

            var resultado = await _service.ProcessAtmCreditCardPaymentAsync(new AtmCreditCardPaymentDto
            {
                SourceAccountNumber = "500000001",
                CreditCardNumber = "4111111111111111",
                Amount = 900m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeTrue();

            //Solo se cobra la deuda real: la tarjeta nunca queda en negativo.
            tarjeta.OwedAmount.Should().Be(0m);
            cuenta.Balance.Should().Be(9_700m);
            _asientos.Should().ContainSingle().Which.Amount.Should().Be(300m);
        }

        [Fact]
        public async Task ProcessAtmCreditCardPaymentAsync_SinDeudaPendiente_Rechaza()
        {
            var cuenta = Cuenta(1, "500000001", 10_000m);
            RegistrarCuentas(cuenta);
            RegistrarTarjeta(Tarjeta("4111111111111111", 0m));

            var resultado = await _service.ProcessAtmCreditCardPaymentAsync(new AtmCreditCardPaymentDto
            {
                SourceAccountNumber = "500000001",
                CreditCardNumber = "4111111111111111",
                Amount = 100m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            cuenta.Balance.Should().Be(10_000m);
            _asientos.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessAtmCreditCardPaymentAsync_SinFondos_RegistraElIntentoRechazadoYNoMueveNada()
        {
            var cuenta = Cuenta(1, "500000001", 50m);
            RegistrarCuentas(cuenta);
            var tarjeta = Tarjeta("4111111111111111", 800m);
            RegistrarTarjeta(tarjeta);

            var resultado = await _service.ProcessAtmCreditCardPaymentAsync(new AtmCreditCardPaymentDto
            {
                SourceAccountNumber = "500000001",
                CreditCardNumber = "4111111111111111",
                Amount = 800m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            cuenta.Balance.Should().Be(50m);
            tarjeta.OwedAmount.Should().Be(800m);

            var rechazado = _asientos.Should().ContainSingle().Subject;
            rechazado.Status.Should().Be(TransactionStatus.Rechazada);
            rechazado.RejectionReason.Should().Be("Fondos insuficientes");

            //El intento rechazado debe quedar guardado, no solo agregado al contexto.
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessAtmCreditCardPaymentAsync_ConTarjetaCancelada_Rechaza()
        {
            var cuenta = Cuenta(1, "500000001", 10_000m);
            RegistrarCuentas(cuenta);
            RegistrarTarjeta(Tarjeta("4111111111111111", 500m, CreditCardStatus.Cancelada));

            var resultado = await _service.ProcessAtmCreditCardPaymentAsync(new AtmCreditCardPaymentDto
            {
                SourceAccountNumber = "500000001",
                CreditCardNumber = "4111111111111111",
                Amount = 100m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            cuenta.Balance.Should().Be(10_000m);
            _asientos.Should().BeEmpty();
        }

        #endregion

        #region transferencia a terceros

        [Fact]
        public async Task ProcessAtmThirdPartyTransferAsync_ConFondos_RegistraElParCruzadoEnUnaSolaConfirmacion()
        {
            var origen = Cuenta(1, "500000001", 3_000m);
            var destino = Cuenta(2, "500000002", 500m, cliente: "cliente-002");
            RegistrarCuentas(origen, destino);

            var resultado = await _service.ProcessAtmThirdPartyTransferAsync(new AtmThirdPartyTransferDto
            {
                SourceAccountNumber = "500000001",
                DestinationAccountNumber = "500000002",
                Amount = 1_000m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeTrue();
            origen.Balance.Should().Be(2_000m);
            destino.Balance.Should().Be(1_500m);

            _asientos.Should().HaveCount(2);

            var debito = _asientos.Single(a => a.TransactionType == TransactionType.Debito);
            var credito = _asientos.Single(a => a.TransactionType == TransactionType.Credito);

            debito.SavingsAccountId.Should().Be(origen.Id);
            credito.SavingsAccountId.Should().Be(destino.Id);
            debito.RelatedTransaction.Should().BeSameAs(credito);
            credito.RelatedTransaction.Should().BeSameAs(debito);

            //Balances y ambos asientos se confirman juntos: nunca queda medio movimiento.
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessAtmThirdPartyTransferAsync_SinFondos_RegistraElRechazoYNoMueveBalances()
        {
            var origen = Cuenta(1, "500000001", 200m);
            var destino = Cuenta(2, "500000002", 500m, cliente: "cliente-002");
            RegistrarCuentas(origen, destino);

            var resultado = await _service.ProcessAtmThirdPartyTransferAsync(new AtmThirdPartyTransferDto
            {
                SourceAccountNumber = "500000001",
                DestinationAccountNumber = "500000002",
                Amount = 1_000m,
                CashierId = CashierId
            });

            resultado.IsValid.Should().BeFalse();
            origen.Balance.Should().Be(200m);
            destino.Balance.Should().Be(500m);

            _asientos.Should().ContainSingle()
                .Which.Status.Should().Be(TransactionStatus.Rechazada);
        }

        #endregion
    }
}
