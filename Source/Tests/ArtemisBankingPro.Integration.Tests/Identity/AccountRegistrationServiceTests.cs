using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Interfaces;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Registration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Identity
{
    //Creación de usuarios contra el UserManager real: estado inicial inactivo, cuenta de
    //ahorro principal automática para clientes y correo de activación obligatorio.
    public sealed class AccountRegistrationServiceTests : IDisposable
    {
        private const string PrimaryAccountNumber = "500000001";

        private readonly IdentityTestHost _host;
        private readonly Mock<IGenerateTokens> _generateTokens;
        private readonly Mock<IEmailServices> _emailServices;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository;
        private readonly Mock<ITransactionRepository> _transactionRepository;
        private readonly AccountRegistrationService _service;

        private readonly List<SavingsAccount> _createdAccounts = new();
        private readonly List<Transaction> _createdTransactions = new();
        private MessageDto? _sentEmail;

        public AccountRegistrationServiceTests()
        {
            _host = new IdentityTestHost();
            _generateTokens = new Mock<IGenerateTokens>();
            _emailServices = new Mock<IEmailServices>();
            _savingsAccountsRepository = new Mock<ISavingsAccountsRepository>();
            _transactionRepository = new Mock<ITransactionRepository>();

            _generateTokens
                .Setup(g => g.GenerateTokenConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser user, string origin) =>
                    $"{origin}/Account/ConfirmAccountEmail?userId={user.Id}&token=token-de-prueba");

            _emailServices.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                .Callback<MessageDto>(message => _sentEmail = message)
                .ReturnsAsync(true);

            //El número de cuenta lo emite la secuencia del módulo de cuentas de ahorro
            _savingsAccountsRepository.Setup(r => r.GetNextAccountNumberAsync())
                .ReturnsAsync(PrimaryAccountNumber);
            _savingsAccountsRepository.Setup(r => r.AddAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync((SavingsAccount account) =>
                {
                    account.Id = _createdAccounts.Count + 1;
                    _createdAccounts.Add(account);
                    return account;
                });
            _savingsAccountsRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _transactionRepository.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction transaction) =>
                {
                    _createdTransactions.Add(transaction);
                    return transaction;
                });
            _transactionRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _service = new AccountRegistrationService(
                _host.UserManager,
                _generateTokens.Object,
                _emailServices.Object,
                _savingsAccountsRepository.Object,
                _transactionRepository.Object,
                NullLogger<AccountRegistrationService>.Instance);
        }

        // ─── Validaciones ────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterUserAsync_WithAnUnknownRole_ShouldRejectTheRegistration()
        {
            var response = await _service.RegisterUserAsync(Request(role: "Supervisor"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("El tipo de usuario seleccionado no es válido.");
        }

        [Fact]
        public async Task RegisterUserAsync_WithMismatchedPasswords_ShouldRejectTheRegistration()
        {
            var request = Request();
            request.ConfirmPassword = "OtraClave2*";

            var response = await _service.RegisterUserAsync(request);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("La contraseña y la confirmación de contraseña deben coincidir.");
        }

        [Fact]
        public async Task RegisterUserAsync_WithANegativeInitialAmount_ShouldRejectTheRegistration()
        {
            var request = Request();
            request.InitialAmount = -0.01m;

            var response = await _service.RegisterUserAsync(request);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("El monto inicial no puede ser negativo.");
            _host.UserManager.Users.Should().BeEmpty();
        }

        [Fact]
        public async Task RegisterUserAsync_WithAnAlreadyRegisteredEmail_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "existente", email: "cliente01@artemisbank.com");

            var response = await _service.RegisterUserAsync(Request());

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe un usuario registrado con este correo electrónico.");
        }

        [Fact]
        public async Task RegisterUserAsync_WithAnAlreadyRegisteredUserName_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", email: "otro@artemisbank.com");

            var response = await _service.RegisterUserAsync(Request());

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe un usuario registrado con este nombre de usuario.");
        }

        [Fact]
        public async Task RegisterUserAsync_WithAnAlreadyRegisteredIdCard_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "otro", email: "otro@artemisbank.com", idCard: "00187654321");

            var response = await _service.RegisterUserAsync(Request());

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe un usuario registrado con esta cédula.");
        }

        // ─── Creación ────────────────────────────────────────────────────────

        //Todo usuario creado desde el sistema queda inactivo hasta completar la activación
        [Fact]
        public async Task RegisterUserAsync_ShouldCreateTheUserInactiveAndWithItsRole()
        {
            var response = await _service.RegisterUserAsync(Request());

            response.HasError.Should().BeFalse();
            var created = await _host.UserManager.FindByIdAsync(response.UserId!);
            created.Should().NotBeNull();
            created!.IsActive.Should().BeFalse();
            created.EmailConfirmed.Should().BeFalse();
            (await _host.UserManager.IsInRoleAsync(created, nameof(Roles.Cliente))).Should().BeTrue();
        }

        [Fact]
        public async Task RegisterUserAsync_ForAClient_ShouldCreateThePrimaryAccountWithTheInitialAmount()
        {
            var request = Request();
            request.InitialAmount = 5000m;

            var response = await _service.RegisterUserAsync(request);

            response.HasError.Should().BeFalse();
            _createdAccounts.Should().ContainSingle();

            var account = _createdAccounts[0];
            account.AccountNumber.Should().Be(PrimaryAccountNumber);
            account.AccountNumber.Should().HaveLength(9);
            account.CustomerId.Should().Be(response.UserId);
            account.Balance.Should().Be(5000m);
            account.AccountType.Should().Be(SavingsAccountType.Principal);
            account.Status.Should().Be(SavingsAccountStatus.Activa);
        }

        //Un monto inicial mayor que cero debe registrarse como transacción de tipo Crédito
        [Fact]
        public async Task RegisterUserAsync_WithAnInitialAmountAboveZero_ShouldRegisterACreditTransaction()
        {
            var request = Request();
            request.InitialAmount = 5000m;

            await _service.RegisterUserAsync(request);

            _createdTransactions.Should().ContainSingle();
            var transaction = _createdTransactions[0];
            transaction.TransactionType.Should().Be(TransactionType.Credito);
            transaction.OperationType.Should().Be(OperationType.AperturaCuenta);
            transaction.Amount.Should().Be(5000m);
            transaction.Status.Should().Be(TransactionStatus.Aprobada);
        }

        //Si el monto inicial es RD$0.00 no es obligatorio registrar transacción
        [Fact]
        public async Task RegisterUserAsync_WithoutAnInitialAmount_ShouldOpenTheAccountAtZeroWithoutTransactions()
        {
            var request = Request();
            request.InitialAmount = null;

            await _service.RegisterUserAsync(request);

            _createdAccounts.Should().ContainSingle().Which.Balance.Should().Be(0m);
            _createdTransactions.Should().BeEmpty();
        }

        [Theory]
        [InlineData(nameof(Roles.Administrador))]
        [InlineData(nameof(Roles.Cajero))]
        public async Task RegisterUserAsync_ForAdministratorsAndCashiers_ShouldNotCreateASavingsAccount(string role)
        {
            var response = await _service.RegisterUserAsync(Request(role: role));

            response.HasError.Should().BeFalse();
            _createdAccounts.Should().BeEmpty();
        }

        // ─── Correo de activación ────────────────────────────────────────────

        //Desde la aplicación web el correo lleva un enlace de activación
        [Fact]
        public async Task RegisterUserAsync_WithAnOrigin_ShouldSendTheActivationLink()
        {
            var request = Request();
            request.Origin = "https://localhost";

            await _service.RegisterUserAsync(request);

            _sentEmail.Should().NotBeNull();
            _sentEmail!.Subject.Should().Be("Activación de cuenta");
            _sentEmail.To.Should().Be(request.Email);
            _sentEmail.Message.Should().Contain("https://localhost/Account/ConfirmAccountEmail?userId=");
        }

        //Desde la Web API el correo lleva el token directamente en el cuerpo, no un enlace
        [Fact]
        public async Task RegisterUserAsync_WithoutAnOrigin_ShouldSendTheTokenInTheBody()
        {
            var request = Request();
            request.Origin = null;

            await _service.RegisterUserAsync(request);

            _sentEmail.Should().NotBeNull();
            _sentEmail!.Message.Should().NotContain("/Account/ConfirmAccountEmail");
            _sentEmail.Message.Should().Contain("Identificador de usuario");
        }

        [Fact]
        public async Task RegisterUserAsync_WhenTheEmailCannotBeSent_ShouldReportItToTheAdministrator()
        {
            _emailServices.Setup(e => e.SendNotification(It.IsAny<MessageDto>())).ReturnsAsync(false);

            var response = await _service.RegisterUserAsync(Request());

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("No fue posible enviar el correo de activación. Intente nuevamente más tarde.");
        }

        // ─── Activación de cuenta ────────────────────────────────────────────

        [Theory]
        [InlineData("", "token")]
        [InlineData("user-1", "")]
        [InlineData("no-existe", "token")]
        public async Task ConfirmAccountAsync_WithoutAValidUserOrToken_ShouldReportAnInvalidLink(string userId, string token)
        {
            var response = await _service.ConfirmAccountAsync(userId, token);

            response.HasError.Should().BeTrue();
            response.Message.Should().Be("El enlace de activación no es válido.");
        }

        [Fact]
        public async Task ConfirmAccountAsync_WithAValidToken_ShouldActivateTheAccount()
        {
            var response = await _service.RegisterUserAsync(Request());
            var user = await _host.UserManager.FindByIdAsync(response.UserId!);
            var token = await _host.UserManager.GenerateEmailConfirmationTokenAsync(user!);

            var confirmation = await _service.ConfirmAccountAsync(user!.Id, token);

            confirmation.HasError.Should().BeFalse();
            confirmation.Message.Should().Be("Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.");
            (await _host.UserManager.FindByIdAsync(user.Id))!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ConfirmAccountAsync_WithAnInvalidToken_ShouldNotActivateTheAccount()
        {
            var response = await _service.RegisterUserAsync(Request());

            var confirmation = await _service.ConfirmAccountAsync(response.UserId!, "token-falso");

            confirmation.HasError.Should().BeTrue();
            confirmation.Message.Should().Be("El enlace de activación no es válido.");
            (await _host.UserManager.FindByIdAsync(response.UserId!))!.IsActive.Should().BeFalse();
        }

        //El token de activación es de un solo uso
        [Fact]
        public async Task ConfirmAccountAsync_OnAnAlreadyActivatedAccount_ShouldReportThatTheLinkWasUsed()
        {
            var response = await _service.RegisterUserAsync(Request());
            var user = await _host.UserManager.FindByIdAsync(response.UserId!);
            var token = await _host.UserManager.GenerateEmailConfirmationTokenAsync(user!);
            await _service.ConfirmAccountAsync(user!.Id, token);

            var second = await _service.ConfirmAccountAsync(user.Id, token);

            second.HasError.Should().BeTrue();
            second.Message.Should().Be("Este enlace de activación ya fue utilizado.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static RegisterRequest Request(string role = nameof(Roles.Cliente))
        {
            return new RegisterRequest
            {
                FirstName = "María",
                LastName = "Gómez",
                IDCARD = "00187654321",
                Email = "cliente01@artemisbank.com",
                UserName = "cliente01",
                Password = "Clave123*",
                ConfirmPassword = "Clave123*",
                Role = role
            };
        }

        public void Dispose() => _host.Dispose();
    }
}
