using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Services.CreditCards;
using Artemis_Banking_Pro.Core.Application.Services.Debts;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Generators;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Security;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Services.CreditCards
{
    //El módulo completo contra persistencia real: servicio de aplicación, servicio de
    //validaciones, repositorios, AutoMapper, generador de números y hasher del CVC.
    //Solo quedan dobles Identity (usuarios y sesión) y el correo, que son fronteras externas.
    public sealed class CreditCardsIntegrationTests : IDisposable
    {
        private const string AdminId = "admin-1";
        private const string CustomerId = "customer-1";
        private const string OtherCustomerId = "customer-2";
        private const string CustomerIdCard = "40200000001";

        private readonly DbContextArtemisBanking _context;
        private readonly CreditCardsRepository _creditCardsRepository;
        private readonly CardConsumptionRepository _cardConsumptionRepository;
        private readonly CreditCardsServices _service;

        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();
        private readonly Mock<IEmailServices> _emailServices = new();

        public CreditCardsIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseInMemoryDatabase($"credit-cards-service-{Guid.NewGuid()}")
                .Options;

            _context = new DbContextArtemisBanking(options);
            _creditCardsRepository = new CreditCardsRepository(_context);
            _cardConsumptionRepository = new CardConsumptionRepository(_context);

            var mapper = new MapperConfiguration(
                configuration =>
                {
                    configuration.AddProfile<CreditCardsMappingEntitieToDtoAndReverse>();
                },
                NullLoggerFactory.Instance).CreateMapper();

            GivenAuthenticatedAdministrator();
            GivenCustomerInIdentity(CustomerId, CustomerIdCard, "Ana", "Pérez", "ana@artemis.com");
            _emailServices.Setup(s => s.SendNotification(It.IsAny<MessageDto>())).ReturnsAsync(true);

            var validationServices = new CreditCardsValidationServices(
                _creditCardsRepository,
                _userManagementService.Object,
                _currentUserService.Object,
                NullLogger<CreditCardsValidationServices>.Instance);

            var debtCalculator = new DebtCalculator(
                new LoansRepository(_context),
                _creditCardsRepository,
                NullLogger<DebtCalculator>.Instance);

            _service = new CreditCardsServices(
                _creditCardsRepository,
                _cardConsumptionRepository,
                validationServices,
                new CardNumberGenerator(_creditCardsRepository, NullLogger<CardNumberGenerator>.Instance),
                new CvcHasher(),
                debtCalculator,
                _userManagementService.Object,
                _emailServices.Object,
                mapper,
                NullLogger<CreditCardsServices>.Instance);
        }

        #region asignación de tarjeta
        //Registro completo: número de 16 dígitos único, CVC hasheado, expiración a 3 años,
        //deuda inicial en RD$0.00 y estado Activa.
        [Fact]
        public async Task AssignCreditCardAsync_ShouldRegisterTheCardWithEverythingTheSystemGenerates()
        {
            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 75_000m
            });

            result.IsValid.Should().BeTrue();

            var card = await _context.CreditCards.SingleAsync();

            card.CardNumber.Should().HaveLength(16).And.MatchRegex("^[0-9]{16}$");
            card.LastFourDigits.Should().Be(card.CardNumber[^4..]);
            card.CustomerId.Should().Be(CustomerId);
            card.CreditLimit.Should().Be(75_000m);
            card.OwedAmount.Should().Be(0m);
            card.Status.Should().Be(CreditCardStatus.Activa);
            card.ExpirationDate.Should().BeCloseTo(DateTimeOffset.UtcNow.AddYears(3), TimeSpan.FromMinutes(1));
        }

        //El CVC nunca se guarda en claro: lo que se persiste es su hash SHA-256 de 64 caracteres.
        [Fact]
        public async Task AssignCreditCardAsync_ShouldPersistTheCvcOnlyAsASha256Hash()
        {
            await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 10_000m
            });

            var card = await _context.CreditCards.SingleAsync();

            card.CvcHash.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");

            //Ningún CVC posible de 3 dígitos coincide con el hash guardado en texto plano
            Enumerable.Range(0, 1000)
                .Select(cvc => cvc.ToString("D3"))
                .Should().NotContain(card.CvcHash);
        }

        //El administrador responsable sale de la sesión, no de un parámetro del formulario.
        [Fact]
        public async Task AssignCreditCardAsync_ShouldAttributeTheCardToTheAdministratorInSession()
        {
            await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 10_000m
            });

            var card = await _context.CreditCards.SingleAsync();

            card.AssignedByAdminId.Should().Be(AdminId);
            card.CreateByUserId.Should().Be(AdminId);
        }

        [Fact]
        public async Task AssignCreditCardAsync_WithoutAnAdministratorInSession_ShouldNotRegisterAnything()
        {
            _currentUserService.SetupGet(s => s.UserId).Returns((string?)null);
            _currentUserService.Setup(s => s.IsInRole(It.IsAny<string>())).Returns(false);

            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 10_000m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.AdminUserRequired);
            (await _context.CreditCards.CountAsync()).Should().Be(0);
        }

        //Solo se asignan tarjetas a clientes activos.
        [Fact]
        public async Task AssignCreditCardAsync_ToAnInactiveCustomer_ShouldBeRejected()
        {
            _userManagementService
                .Setup(s => s.ValidateUserExistsByIdAsync(CustomerId))
                .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = false });

            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 10_000m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.CustomerIsNotActive);
            (await _context.CreditCards.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task AssignCreditCardAsync_ToACustomerThatDoesNotExist_ShouldBeRejected()
        {
            _userManagementService
                .Setup(s => s.ValidateUserExistsByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserExistenceDto { Exists = false, IsActive = false });

            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = "customer-fantasma",
                CreditLimit = 10_000m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCustomerByIdCard);
            (await _context.CreditCards.CountAsync()).Should().Be(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1500)]
        public async Task AssignCreditCardAsync_WithALimitOfZeroOrLess_ShouldBeRejected(decimal creditLimit)
        {
            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = creditLimit
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.InvalidCreditLimitAssignment);
            (await _context.CreditCards.CountAsync()).Should().Be(0);
        }

        //Un cliente puede tener varias tarjetas y cada número es distinto del anterior.
        [Fact]
        public async Task AssignCreditCardAsync_ShouldAllowSeveralCardsPerCustomerWithUniqueNumbers()
        {
            for (var assignment = 0; assignment < 5; assignment++)
            {
                var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
                {
                    CustomerId = CustomerId,
                    CreditLimit = 10_000m
                });

                result.IsValid.Should().BeTrue();
            }

            var cards = await _context.CreditCards.ToListAsync();

            cards.Should().HaveCount(5);
            cards.Select(card => card.CardNumber).Should().OnlyHaveUniqueItems();
        }

        //El correo se envía después de confirmar la tarjeta y solo lleva los últimos 4 dígitos.
        [Fact]
        public async Task AssignCreditCardAsync_ShouldNotifyTheCustomerWithoutExposingSensitiveData()
        {
            MessageDto? sent = null;
            _emailServices
                .Setup(s => s.SendNotification(It.IsAny<MessageDto>()))
                .Callback<MessageDto>(message => sent = message)
                .ReturnsAsync(true);

            await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 25_000m
            });

            var card = await _context.CreditCards.SingleAsync();

            sent.Should().NotBeNull();
            sent!.To.Should().Be("ana@artemis.com");
            sent.Subject.Should().Be("Nueva tarjeta de crédito asignada");
            sent.Message.Should().Contain(card.LastFourDigits);
            sent.Message.Should().NotContain(card.CardNumber);
            sent.Message.Should().NotContain(card.CvcHash);
        }

        //Un fallo de correo no revierte la tarjeta creada.
        [Fact]
        public async Task AssignCreditCardAsync_WhenTheEmailFails_ShouldKeepTheCard()
        {
            _emailServices.Setup(s => s.SendNotification(It.IsAny<MessageDto>())).ReturnsAsync(false);

            var result = await _service.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = CustomerId,
                CreditLimit = 25_000m
            });

            result.IsValid.Should().BeTrue();
            (await _context.CreditCards.CountAsync()).Should().Be(1);
        }
        #endregion

        #region edición del límite
        [Fact]
        public async Task EditCreditCardLimitAsync_ShouldUpdateTheLimitAndStampTheAdministrator()
        {
            var card = await GivenCard(CreditCardStatus.Activa, creditLimit: 20_000m, owedAmount: 5_000m);

            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = card.Id,
                CreditLimit = 30_000m
            });

            result.IsValid.Should().BeTrue();
            result.Value!.CreditLimit.Should().Be(30_000m);

            _context.ChangeTracker.Clear();
            var updated = await _context.CreditCards.SingleAsync();

            updated.CreditLimit.Should().Be(30_000m);
            updated.LastModifiedByIdUser.Should().Be(AdminId);
            updated.ModifiedAt.Should().NotBeNull();

            //La deuda no se toca al mover el límite
            updated.OwedAmount.Should().Be(5_000m);
        }

        //Se permite disminuir el límite mientras no quede por debajo de la deuda actual.
        [Fact]
        public async Task EditCreditCardLimitAsync_ShouldAllowLoweringTheLimitDownToTheOwedAmount()
        {
            var card = await GivenCard(CreditCardStatus.Activa, creditLimit: 20_000m, owedAmount: 8_000m);

            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = card.Id,
                CreditLimit = 8_000m
            });

            result.IsValid.Should().BeTrue();

            _context.ChangeTracker.Clear();
            (await _context.CreditCards.SingleAsync()).CreditLimit.Should().Be(8_000m);
        }

        [Fact]
        public async Task EditCreditCardLimitAsync_BelowTheOwedAmount_ShouldBeRejected()
        {
            var card = await GivenCard(CreditCardStatus.Activa, creditLimit: 20_000m, owedAmount: 8_000m);

            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = card.Id,
                CreditLimit = 7_999m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.CreditLimitLowerThanOwedAmount);

            _context.ChangeTracker.Clear();
            (await _context.CreditCards.SingleAsync()).CreditLimit.Should().Be(20_000m);
        }

        [Fact]
        public async Task EditCreditCardLimitAsync_OnACancelledCard_ShouldBeRejected()
        {
            var card = await GivenCard(CreditCardStatus.Cancelada, creditLimit: 20_000m);

            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = card.Id,
                CreditLimit = 30_000m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.CreditCardIsCancelled);
        }

        [Fact]
        public async Task EditCreditCardLimitAsync_OnACardThatDoesNotExist_ShouldBeRejected()
        {
            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = 9999,
                CreditLimit = 30_000m
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCreditCard);
        }

        //Un fallo de correo no revierte el nuevo límite.
        [Fact]
        public async Task EditCreditCardLimitAsync_WhenTheEmailFails_ShouldKeepTheNewLimit()
        {
            _emailServices.Setup(s => s.SendNotification(It.IsAny<MessageDto>())).ReturnsAsync(false);

            var card = await GivenCard(CreditCardStatus.Activa, creditLimit: 20_000m);

            var result = await _service.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = card.Id,
                CreditLimit = 45_000m
            });

            result.IsValid.Should().BeTrue();

            _context.ChangeTracker.Clear();
            (await _context.CreditCards.SingleAsync()).CreditLimit.Should().Be(45_000m);
        }

        //El formulario de edición llega precargado con el límite vigente.
        [Fact]
        public async Task GetCreditCardForEditLimitAsync_ShouldPreloadTheCurrentLimit()
        {
            var card = await GivenCard(CreditCardStatus.Activa, creditLimit: 33_000m);

            var result = await _service.GetCreditCardForEditLimitAsync(card.Id);

            result.IsValid.Should().BeTrue();
            result.Value!.Id.Should().Be(card.Id);
            result.Value.CreditLimit.Should().Be(33_000m);
        }
        #endregion

        #region cancelación
        [Fact]
        public async Task CancelCreditCardAsync_WithoutDebt_ShouldOnlyChangeTheStatus()
        {
            var card = await GivenCard(CreditCardStatus.Activa, owedAmount: 0m);
            await GivenConsumption(card.Id, 400m, ConsumptionStatus.Aprobado);

            var result = await _service.CancelCreditCardAsync(card.Id);

            result.IsValid.Should().BeTrue();

            _context.ChangeTracker.Clear();
            var cancelled = await _context.CreditCards.SingleAsync();

            cancelled.Status.Should().Be(CreditCardStatus.Cancelada);
            cancelled.LastModifiedByIdUser.Should().Be(AdminId);

            //Cancelar no elimina la tarjeta ni su historial de consumos
            (await _context.CreditCards.CountAsync()).Should().Be(1);
            (await _context.CardConsumptions.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CancelCreditCardAsync_WithPendingDebt_ShouldBeRejected()
        {
            var card = await GivenCard(CreditCardStatus.Activa, owedAmount: 0.01m);

            var result = await _service.CancelCreditCardAsync(card.Id);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.CreditCardWithPendingDebt);

            _context.ChangeTracker.Clear();
            (await _context.CreditCards.SingleAsync()).Status.Should().Be(CreditCardStatus.Activa);
        }

        [Fact]
        public async Task CancelCreditCardAsync_OnAnAlreadyCancelledCard_ShouldBeRejected()
        {
            var card = await GivenCard(CreditCardStatus.Cancelada);

            var result = await _service.CancelCreditCardAsync(card.Id);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.CreditCardIsCancelled);
        }
        #endregion

        #region listado, búsqueda y detalles
        //Por defecto el listado muestra solo las activas, de la más reciente a la más antigua.
        [Fact]
        public async Task GetPagedCreditCardsAsync_ByDefault_ShouldOnlyReturnActiveCards()
        {
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110001");
            await GivenCard(CreditCardStatus.Cancelada, cardNumber: "1111111111110002");

            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto());

            result.IsValid.Should().BeTrue();
            result.Value!.Items.Should().ContainSingle()
                .Which.StateCreditCard.Should().Be(CreditCardStatus.Activa);
        }

        //El listado nunca expone el número completo ni el hash del CVC: solo el enmascarado.
        [Fact]
        public async Task GetPagedCreditCardsAsync_ShouldOnlyExposeTheMaskedNumberAndTheLastFourDigits()
        {
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1234567890123456",
                creditLimit: 10_000m, owedAmount: 2_500m);

            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto());

            var card = result.Value!.Items.Single();

            card.MaskedCardNumber.Should().Be("**** **** **** 3456");
            card.LastFourDigits.Should().Be("3456");
            card.AvailableCredit.Should().Be(7_500m);
            card.ExpirationDate.Should().MatchRegex(@"^\d{2}/\d{2}$");

            //El nombre del titular lo completa Identity
            card.FullNameCustomer.Should().Be("Ana Pérez");
        }

        //La cédula del filtro se traduce al Id del cliente antes de consultar.
        [Fact]
        public async Task GetPagedCreditCardsAsync_SearchingByIdCard_ShouldOnlyReturnThatCustomerCards()
        {
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110001");
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110002", customerId: OtherCustomerId);

            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                IdCard = CustomerIdCard,
                Status = CreditCardStatusFilter.Todas
            });

            result.IsValid.Should().BeTrue();
            result.Value!.Items.Should().ContainSingle()
                .Which.CustomerId.Should().Be(CustomerId);
        }

        //Buscando por cédula sin estado: activas primero, luego canceladas.
        [Fact]
        public async Task GetPagedCreditCardsAsync_SearchingByIdCardWithoutStatus_ShouldShowActiveCardsFirst()
        {
            var today = DateTimeOffset.UtcNow;

            await GivenCard(CreditCardStatus.Cancelada, cardNumber: "1111111111110001", createdAt: today);
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110002", createdAt: today.AddDays(-10));
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110003", createdAt: today.AddDays(-1));

            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                IdCard = CustomerIdCard,
                Status = CreditCardStatusFilter.Todas
            });

            result.Value!.Items.Select(card => card.LastFourDigits)
                .Should().Equal("0003", "0002", "0001");
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_WithAnIdCardThatDoesNotExist_ShouldReportIt()
        {
            _userManagementService
                .Setup(s => s.GetClientByIdCardAsync(It.IsAny<string>()))
                .ReturnsAsync((ClientSummaryDto?)null);

            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                IdCard = "00000000000"
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCustomerByIdCard);
        }

        [Fact]
        public async Task GetPagedCreditCardsAsync_WhenTheCustomerHasNoCards_ShouldReportIt()
        {
            var result = await _service.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                IdCard = CustomerIdCard,
                Status = CreditCardStatusFilter.Todas
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCreditCards);
        }

        [Fact]
        public async Task GetPagedConsumptionsAsync_ShouldReturnApprovedAndRejectedConsumptions()
        {
            var card = await GivenCard(CreditCardStatus.Activa);
            var today = DateTimeOffset.UtcNow;

            await GivenConsumption(card.Id, 500m, ConsumptionStatus.Aprobado, createdAt: today.AddDays(-2));
            await GivenConsumption(card.Id, 900m, ConsumptionStatus.Rechazado,
                RejectionReason.CreditoInsuficiente, createdAt: today.AddDays(-1));

            var result = await _service.GetPagedConsumptionsAsync(card.Id, 1);

            result.IsValid.Should().BeTrue();
            result.Value!.Items.Select(consumption => consumption.Amount).Should().Equal(900m, 500m);
            result.Value.Items.Should().Contain(consumption =>
                consumption.StateConsumption == ConsumptionStatus.Rechazado);
        }

        [Fact]
        public async Task GetPagedConsumptionsAsync_OnACardThatDoesNotExist_ShouldBeRejected()
        {
            var result = await _service.GetPagedConsumptionsAsync(9999, 1);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCreditCard);
        }
        #endregion

        #region paso 1 de la asignación
        //Arriba del listado va la deuda promedio del sistema; cada fila lleva la deuda del
        //cliente, que suma préstamos activos y tarjetas activas.
        [Fact]
        public async Task GetCustomersForAssignmentAsync_ShouldReturnActiveClientsWithTheirDebtAndTheAverage()
        {
            GivenActiveClientsInIdentity();

            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110001", owedAmount: 3_000m);
            await GivenCard(CreditCardStatus.Cancelada, cardNumber: "1111111111110002", owedAmount: 9_999m);
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110003",
                customerId: OtherCustomerId, owedAmount: 1_000m);
            await GivenActiveLoan(CustomerId, pendingAmount: 2_000m);

            var result = await _service.GetCustomersForAssignmentAsync(null);

            result.IsValid.Should().BeTrue();

            var clients = result.Value!.Clients;

            clients.Should().HaveCount(2);

            //Préstamo activo (2.000) + tarjeta activa (3.000); la cancelada no cuenta
            clients.Single(client => client.Id == CustomerId).TotalDebtAmount.Should().Be(5_000m);
            clients.Single(client => client.Id == OtherCustomerId).TotalDebtAmount.Should().Be(1_000m);

            //(5.000 + 1.000) / 2 clientes activos
            result.Value.AverageDebt.Should().Be(3_000m);
        }

        //A diferencia de préstamos, tener una tarjeta activa no descarta al cliente.
        [Fact]
        public async Task GetCustomersForAssignmentAsync_ShouldKeepCustomersThatAlreadyHaveAnActiveCard()
        {
            GivenActiveClientsInIdentity();
            await GivenCard(CreditCardStatus.Activa, cardNumber: "1111111111110001");

            var result = await _service.GetCustomersForAssignmentAsync(null);

            result.Value!.Clients.Should().Contain(client => client.Id == CustomerId);
        }

        [Fact]
        public async Task GetCustomersForAssignmentAsync_SearchingByIdCard_ShouldNarrowTheListToThatCustomer()
        {
            GivenActiveClientsInIdentity();

            var result = await _service.GetCustomersForAssignmentAsync(CustomerIdCard);

            result.IsValid.Should().BeTrue();
            result.Value!.Clients.Should().ContainSingle()
                .Which.IdCard.Should().Be(CustomerIdCard);
        }

        [Fact]
        public async Task GetCustomersForAssignmentAsync_WithAnIdCardThatDoesNotExist_ShouldReportIt()
        {
            _userManagementService
                .Setup(s => s.GetClientByIdCardAsync(It.IsAny<string>()))
                .ReturnsAsync((ClientSummaryDto?)null);

            var result = await _service.GetCustomersForAssignmentAsync("00000000000");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CreditCardError.NonExistsCustomerByIdCard);
        }
        #endregion

        #region composición de AutoMapper
        //El registro real de la aplicación carga todos los perfiles del ensamblado a la vez.
        //Hoy conviven dos perfiles que declaran CreditCard -> CreditCardDto (el del módulo y el
        //del dashboard del cliente): esta prueba fija el resultado de esa composición para que
        //un cambio en cualquiera de los dos no vacíe el crédito disponible ni el enmascarado.
        [Fact]
        public void TheWholeAssemblyMappingConfiguration_ShouldStillProjectCreditCardsCorrectly()
        {
            var configuration = new MapperConfiguration(
                expression => expression.AddMaps(typeof(CreditCardsMappingEntitieToDtoAndReverse).Assembly),
                NullLoggerFactory.Instance);

            var mapper = configuration.CreateMapper();

            var dto = mapper.Map<CreditCardDto>(new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = CustomerId,
                CreditLimit = 10_000m,
                OwedAmount = 2_500m,
                ExpirationDate = new DateTimeOffset(2029, 5, 1, 0, 0, 0, TimeSpan.Zero),
                CvcHash = new string('a', 64),
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = AdminId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = AdminId
            });

            dto.MaskedCardNumber.Should().Be("**** **** **** 3456");
            dto.ExpirationDate.Should().Be("05/29");
            dto.AvailableCredit.Should().Be(7_500m);
            dto.StateCreditCard.Should().Be(CreditCardStatus.Activa);
        }
        #endregion

        public void Dispose() => _context.Dispose();

        #region helpers
        private void GivenAuthenticatedAdministrator()
        {
            _currentUserService.SetupGet(s => s.UserId).Returns(AdminId);
            _currentUserService.Setup(s => s.IsInRole(Roles.Administrador.ToString())).Returns(true);
        }

        private void GivenCustomerInIdentity(
            string customerId, string idCard, string name, string lastName, string email)
        {
            _userManagementService
                .Setup(s => s.ValidateUserExistsByIdAsync(customerId))
                .ReturnsAsync(new UserExistenceDto { Exists = true, IsActive = true });

            _userManagementService
                .Setup(s => s.GetUserByIdAsync(customerId))
                .ReturnsAsync(new UserDetailDto
                {
                    Id = customerId,
                    UserName = email,
                    Name = name,
                    LastName = lastName,
                    IDCARD = idCard,
                    Email = email,
                    TypeUser = Roles.Cliente,
                    State = true,
                    IsClient = true
                });

            _userManagementService
                .Setup(s => s.GetClientByIdCardAsync(idCard))
                .ReturnsAsync(new ClientSummaryDto
                {
                    Id = customerId,
                    IDCARD = idCard,
                    FullName = $"{name} {lastName}",
                    Email = email
                });
        }

        private void GivenActiveClientsInIdentity()
        {
            GivenCustomerInIdentity(OtherCustomerId, "40200000002", "Luis", "Gómez", "luis@artemis.com");

            _userManagementService
                .Setup(s => s.GetActiveClientsAsync())
                .ReturnsAsync(new List<ClientSummaryDto>
                {
                    new() { Id = CustomerId, IDCARD = CustomerIdCard, FullName = "Ana Pérez", Email = "ana@artemis.com" },
                    new() { Id = OtherCustomerId, IDCARD = "40200000002", FullName = "Luis Gómez", Email = "luis@artemis.com" }
                });
        }

        private async Task<CreditCard> GivenCard(
            CreditCardStatus status,
            string cardNumber = "1111111111119999",
            string customerId = CustomerId,
            decimal creditLimit = 20_000m,
            decimal owedAmount = 0m,
            DateTimeOffset? createdAt = null)
        {
            var card = new CreditCard
            {
                CardNumber = cardNumber,
                LastFourDigits = cardNumber[^4..],
                CustomerId = customerId,
                CreditLimit = creditLimit,
                OwedAmount = owedAmount,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CvcHash = new string('a', 64),
                Status = status,
                AssignedByAdminId = AdminId,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = AdminId
            };

            await _context.CreditCards.AddAsync(card);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            return card;
        }

        private async Task GivenConsumption(
            int creditCardId,
            decimal amount,
            ConsumptionStatus status,
            RejectionReason? rejectionReason = null,
            DateTimeOffset? createdAt = null)
        {
            await _context.CardConsumptions.AddAsync(new CardConsumption
            {
                CreditCardId = creditCardId,
                Amount = amount,
                Origin = ConsumptionOrigin.Comercio,
                CommerceName = "Comercio Artemis",
                Status = status,
                RejectionReason = rejectionReason,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
                CreateByUserId = "SYSTEM"
            });

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private async Task GivenActiveLoan(string customerId, decimal pendingAmount)
        {
            await _context.Loans.AddAsync(new Loan
            {
                LoanNumber = "100000001",
                CustomerId = customerId,
                ApprovedCapital = pendingAmount,
                AnnualInterestRate = 0m,
                termMonths = TermMonths.Meses6,
                MonthlyInstallment = pendingAmount / 6,
                TotalPayable = pendingAmount,
                PendingAmount = pendingAmount,
                Status = LoanStatus.Activo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = AdminId
            });

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
        #endregion
    }
}
