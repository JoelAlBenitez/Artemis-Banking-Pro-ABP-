using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.Login;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ResetPassword;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetAllCommerces;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetAllCreditCards;
using Artemis_Banking_Pro.Core.Application.Features.HermesPay.Commands.ProcessPayment;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.CreateLoan;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetAllLoans;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CreateSecondaryAccount;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.UpdateUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetAllUsers;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using FluentAssertions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Validators
{
    //Validaciones estructurales: lo que el ValidationBehavior rechaza antes de llegar al
    //handler. Nada de lo que se prueba aquí necesita ir a la base de datos.
    public sealed class ApiValidatorsTests
    {
        #region Paginación

        //Las tres reglas se repiten en todos los listados: página > 0, tamaño > 0 y máximo 20.
        [Theory]
        [InlineData(0, 20, false)]
        [InlineData(-1, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 21, false)]
        [InlineData(1, 20, true)]
        [InlineData(3, 5, true)]
        public void GetAllUsers_ShouldEnforceThePaginationLimits(int page, int pageSize, bool expected)
        {
            var result = new GetAllUsersQueryValidator()
                .Validate(new GetAllUsersQuery { Page = page, PageSize = pageSize });

            result.IsValid.Should().Be(expected);
        }

        [Fact]
        public void GetAllUsers_WithPageSizeAboveTheMaximum_ShouldExplainTheLimit()
        {
            var result = new GetAllUsersQueryValidator()
                .Validate(new GetAllUsersQuery { PageSize = 50 });

            result.Errors.Select(error => error.ErrorMessage)
                .Should().Contain("La cantidad máxima de registros por página es 20.");
        }

        //Cada listado repite el mismo contrato de paginación.
        [Fact]
        public void EveryListing_ShouldRejectAPageSizeAboveTwenty()
        {
            new GetAllCommercesQueryValidator()
                .Validate(new GetAllCommercesQuery { PageSize = 21 }).IsValid.Should().BeFalse();

            new GetAllLoansQueryValidator()
                .Validate(new GetAllLoansQuery { PageSize = 21 }).IsValid.Should().BeFalse();

            new GetAllCreditCardsQueryValidator()
                .Validate(new GetAllCreditCardsQuery { PageSize = 21 }).IsValid.Should().BeFalse();

            new GetAllSavingsAccountsQueryValidator()
                .Validate(new GetAllSavingsAccountsQuery { PageSize = 21 }).IsValid.Should().BeFalse();
        }

        #endregion

        #region Filtros con valores cerrados

        [Theory]
        [InlineData(null, true)]
        [InlineData("administrador", true)]
        [InlineData("Cajero", true)]
        [InlineData("CLIENTE", true)]
        [InlineData("Comercio", false)]
        [InlineData("supervisor", false)]
        public void GetAllUsers_ShouldOnlyAcceptTheThreeWebAppRoles(string? role, bool expected)
        {
            var result = new GetAllUsersQueryValidator().Validate(new GetAllUsersQuery { Role = role });

            result.IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("activo", true)]
        [InlineData("inactivo", true)]
        [InlineData("todos", true)]
        [InlineData("activa", false)]
        public void GetAllCommerces_ShouldOnlyAcceptTheDocumentedStatuses(string? status, bool expected)
            => new GetAllCommercesQueryValidator()
                .Validate(new GetAllCommercesQuery { Status = status }).IsValid.Should().Be(expected);

        [Theory]
        [InlineData("activos", true)]
        [InlineData("completados", true)]
        [InlineData("todos", true)]
        [InlineData("activa", false)]
        public void GetAllLoans_ShouldOnlyAcceptTheDocumentedStatuses(string status, bool expected)
            => new GetAllLoansQueryValidator()
                .Validate(new GetAllLoansQuery { Status = status }).IsValid.Should().Be(expected);

        [Theory]
        [InlineData("activa", null, true)]
        [InlineData("cancelada", "principal", true)]
        [InlineData("todas", "secundaria", true)]
        [InlineData("activos", null, false)]
        [InlineData("activa", "terciaria", false)]
        public void GetAllSavingsAccounts_ShouldOnlyAcceptTheDocumentedFilters(
            string status, string? type, bool expected)
            => new GetAllSavingsAccountsQueryValidator()
                .Validate(new GetAllSavingsAccountsQuery { Status = status, Type = type })
                .IsValid.Should().Be(expected);

        #endregion

        #region Account

        [Fact]
        public void Login_WithoutCredentials_ShouldReportBothFields()
        {
            var result = new LoginCommandValidator()
                .Validate(new LoginCommand { UserName = string.Empty, Password = string.Empty });

            result.Errors.Should().HaveCount(2);
        }

        [Fact]
        public void ResetPassword_WithMismatchedPasswords_ShouldRejectTheRequest()
        {
            var result = new ResetPasswordCommandValidator().Validate(new ResetPasswordCommand
            {
                UserId = "1",
                Token = "token",
                Password = "123P@$$word!",
                ConfirmPassword = "otra-cosa"
            });

            result.Errors.Select(error => error.ErrorMessage)
                .Should().Contain("La contraseña y la confirmación de contraseña no coinciden.");
        }

        #endregion

        #region Usuarios

        //El rol Comercio se crea solo desde su endpoint dedicado.
        [Theory]
        [InlineData("Administrador", true)]
        [InlineData("Cajero", true)]
        [InlineData("Cliente", true)]
        [InlineData("Comercio", false)]
        public void CreateUser_ShouldRejectTheCommerceRole(string role, bool expected)
        {
            var command = BuildCreateUserCommand();
            command.Role = role;

            new CreateUserCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        [Fact]
        public void CreateUser_WithNegativeInitialAmount_ShouldRejectTheRequest()
        {
            var command = BuildCreateUserCommand();
            command.InitialAmount = -1m;

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        //El monto inicial es opcional: no enviarlo es válido.
        [Fact]
        public void CreateUser_WithoutInitialAmount_ShouldBeValid()
        {
            var command = BuildCreateUserCommand();
            command.InitialAmount = null;

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        //La contraseña es opcional al actualizar, pero si se envía exige su confirmación.
        [Theory]
        [InlineData(null, null, true)]
        [InlineData("123P@$$word!", "123P@$$word!", true)]
        [InlineData("123P@$$word!", "otra", false)]
        [InlineData("123P@$$word!", null, false)]
        public void UpdateUser_ShouldRequireTheConfirmationOnlyWhenThePasswordTravels(
            string? password, string? confirmation, bool expected)
        {
            var command = BuildUpdateUserCommand();
            command.Password = password;
            command.ConfirmPassword = confirmation;

            new UpdateUserCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        #endregion

        #region Comercios

        [Fact]
        public void CreateCommerce_WithInvalidEmail_ShouldRejectTheRequest()
        {
            var result = new CreateCommerceCommandValidator().Validate(new CreateCommerceCommand
            {
                Name = "Tienda Demo",
                Email = "correo-sin-formato",
                PhoneNumber = "8095551234",
                Rnc = "101999999"
            });

            result.IsValid.Should().BeFalse();
        }

        //La descripción es el único campo opcional del comercio.
        [Fact]
        public void CreateCommerce_WithoutDescription_ShouldBeValid()
        {
            var result = new CreateCommerceCommandValidator().Validate(new CreateCommerceCommand
            {
                Name = "Tienda Demo",
                Description = null,
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999"
            });

            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Préstamos y tarjetas

        //Solo los diez plazos que fija el documento.
        [Theory]
        [InlineData(6, true)]
        [InlineData(12, true)]
        [InlineData(60, true)]
        [InlineData(7, false)]
        [InlineData(0, false)]
        [InlineData(72, false)]
        public void CreateLoan_ShouldOnlyAcceptTheAllowedTerms(int term, bool expected)
        {
            var result = new CreateLoanCommandValidator().Validate(new CreateLoanCommand
            {
                ClientId = "cliente-1",
                CapitalAmount = 100_000m,
                TermInMonths = term,
                AnnualInterestRate = 12m
            });

            result.IsValid.Should().Be(expected);
        }

        [Fact]
        public void CreateLoan_WithNegativeRate_ShouldRejectTheRequest()
        {
            var result = new CreateLoanCommandValidator().Validate(new CreateLoanCommand
            {
                ClientId = "cliente-1",
                CapitalAmount = 100_000m,
                TermInMonths = (int)TermMonths.Meses12,
                AnnualInterestRate = -1m
            });

            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(-100, false)]
        [InlineData(0.01, true)]
        public void CreateCreditCard_ShouldRequireALimitAboveZero(decimal limit, bool expected)
            => new CreateCreditCardCommandValidator()
                .Validate(new CreateCreditCardCommand { ClientId = "cliente-1", CreditLimit = limit })
                .IsValid.Should().Be(expected);

        [Theory]
        [InlineData(0, true)]
        [InlineData(5000, true)]
        [InlineData(-1, false)]
        public void CreateSecondaryAccount_ShouldAllowZeroButNotNegativeBalance(decimal balance, bool expected)
            => new CreateSecondaryAccountCommandValidator()
                .Validate(new CreateSecondaryAccountCommand { ClientId = "cliente-1", InitialBalance = balance })
                .IsValid.Should().Be(expected);

        #endregion

        #region Hermes Pay

        [Theory]
        [InlineData("1589963258467598", true)]
        [InlineData("158996325846759", false)]
        [InlineData("15899632584675988", false)]
        [InlineData("15899632584675XX", false)]
        public void ProcessPayment_ShouldRequireSixteenDigitsInTheCardNumber(string cardNumber, bool expected)
        {
            var command = BuildProcessPaymentCommand();
            command.CardNumber = cardNumber;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("01", true)]
        [InlineData("12", true)]
        [InlineData("00", false)]
        [InlineData("13", false)]
        [InlineData("ab", false)]
        public void ProcessPayment_ShouldRequireAMonthBetweenOneAndTwelve(string month, bool expected)
        {
            var command = BuildProcessPaymentCommand();
            command.MonthExpirationCard = month;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("859", true)]
        [InlineData("85", false)]
        [InlineData("8590", false)]
        [InlineData("85a", false)]
        public void ProcessPayment_ShouldRequireThreeDigitsInTheCvc(string cvc, bool expected)
        {
            var command = BuildProcessPaymentCommand();
            command.Cvc = cvc;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(689.25, true)]
        public void ProcessPayment_ShouldRequireAnAmountAboveZero(decimal amount, bool expected)
        {
            var command = BuildProcessPaymentCommand();
            command.TransactionAmount = amount;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().Be(expected);
        }

        #endregion

        #region builders

        private static CreateUserCommand BuildCreateUserCommand()
            => new()
            {
                FirstName = "Maria",
                LastName = "Gomez",
                Identification = "00187654321",
                Email = "cliente01@artemis.com",
                UserName = "cliente01",
                Password = "123P@$$word!",
                ConfirmPassword = "123P@$$word!",
                Role = nameof(Roles.Cliente)
            };

        private static UpdateUserCommand BuildUpdateUserCommand()
            => new()
            {
                Id = "2",
                FirstName = "Maria",
                LastName = "Gomez",
                Identification = "00187654321",
                Email = "cliente01@artemis.com",
                UserName = "cliente01"
            };

        private static ProcessPaymentCommand BuildProcessPaymentCommand()
            => new()
            {
                CommerceId = 5,
                CardNumber = "1589963258467598",
                MonthExpirationCard = "02",
                YearExpirationCard = "2028",
                Cvc = "859",
                TransactionAmount = 689.25m
            };

        #endregion
    }
}
