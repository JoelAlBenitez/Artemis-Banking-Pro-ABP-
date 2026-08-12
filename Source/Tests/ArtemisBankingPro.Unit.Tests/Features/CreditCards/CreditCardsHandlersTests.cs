using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.UpdateCreditCardLimit;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetAllCreditCards;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetCreditCardById;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.CreditCards
{
    public sealed class CreditCardsHandlersTests
    {
        private const string ClientId = "cliente-1";

        private readonly Mock<ICreditCardsServices> _creditCardsServices = new();

        #region Listado

        //El número completo nunca sale del sistema: solo el enmascarado y los últimos cuatro.
        [Fact]
        public async Task GetAllCreditCards_ShouldExposeOnlyTheMaskedNumber()
        {
            GivenPagedCards(BuildCardDto());

            var handler = new GetAllCreditCardsQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllCreditCardsQuery(), CancellationToken.None);

            var card = result.Data.Single();
            card.MaskedCardNumber.Should().Be("************1234");
            card.LastFourDigits.Should().Be("1234");
            card.CurrentDebt.Should().Be(15_000m);
            card.AvailableCredit.Should().Be(35_000m);
            card.Status.Should().Be(nameof(CreditCardStatus.Activa));
        }

        [Fact]
        public async Task GetAllCreditCards_ShouldTranslateTheStatusFilter()
        {
            GivenPagedCards();

            var handler = new GetAllCreditCardsQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            await handler.Handle(new GetAllCreditCardsQuery { Status = "cancelada" }, CancellationToken.None);

            _creditCardsServices.Verify(service => service.GetPagedCreditCardsAsync(
                It.Is<CreditCardFilterDto>(filter =>
                    filter.Status == CreditCardStatusFilter.Canceladas)), Times.Once);
        }

        [Fact]
        public async Task GetAllCreditCards_WithUnknownIdentification_ShouldReturnAnEmptyPage()
        {
            _creditCardsServices
                .Setup(service => service.GetPagedCreditCardsAsync(It.IsAny<CreditCardFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<CreditCardDto>>.Failure(
                    CreditCardError.NonExistsCustomerByIdCard));

            var handler = new GetAllCreditCardsQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(
                new GetAllCreditCardsQuery { Identification = "00000000000" }, CancellationToken.None);

            result.Data.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.Page.Should().Be(1);
        }

        #endregion

        #region Detalle

        [Fact]
        public async Task GetCreditCardById_ShouldIncludeTheConsumptions()
        {
            _creditCardsServices
                .Setup(service => service.GetByIdAsync(1))
                .ReturnsAsync(ValidationResult<CreditCardDto>.Success(BuildCardDto()));

            _creditCardsServices
                .Setup(service => service.GetPagedConsumptionsAsync(1, 1, It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<PagedResult<CardConsumptionDto>>.Success(
                    new PagedResult<CardConsumptionDto>(
                        [BuildConsumption("Supermercado Demo", ConsumptionStatus.Aprobado)], 1, 20, 1)));

            var handler = new GetCreditCardByIdQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCreditCardByIdQuery { Id = 1 }, CancellationToken.None);

            result.MaskedCardNumber.Should().Be("************1234");
            result.Consumptions.Should().HaveCount(1);
            result.Consumptions.Single().CommerceName.Should().Be("Supermercado Demo");
        }

        //Aprobados y rechazados conviven en el historial, escritos como los nombra el documento.
        [Theory]
        [InlineData(ConsumptionStatus.Aprobado, "APROBADO")]
        [InlineData(ConsumptionStatus.Rechazado, "RECHAZADO")]
        public async Task GetCreditCardById_ShouldWriteTheConsumptionStatusAsTheDocumentDoes(
            ConsumptionStatus status, string expected)
        {
            _creditCardsServices
                .Setup(service => service.GetByIdAsync(1))
                .ReturnsAsync(ValidationResult<CreditCardDto>.Success(BuildCardDto()));

            _creditCardsServices
                .Setup(service => service.GetPagedConsumptionsAsync(1, 1, It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<PagedResult<CardConsumptionDto>>.Success(
                    new PagedResult<CardConsumptionDto>([BuildConsumption("Comercio", status)], 1, 20, 1)));

            var handler = new GetCreditCardByIdQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCreditCardByIdQuery { Id = 1 }, CancellationToken.None);

            result.Consumptions.Single().Status.Should().Be(expected);
        }

        //Un avance de efectivo se muestra con el literal AVANCE en lugar de un comercio.
        [Fact]
        public async Task GetCreditCardById_WithCashAdvance_ShouldShowTheAdvanceLiteral()
        {
            _creditCardsServices
                .Setup(service => service.GetByIdAsync(1))
                .ReturnsAsync(ValidationResult<CreditCardDto>.Success(BuildCardDto()));

            _creditCardsServices
                .Setup(service => service.GetPagedConsumptionsAsync(1, 1, It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<PagedResult<CardConsumptionDto>>.Success(
                    new PagedResult<CardConsumptionDto>(
                        [BuildConsumption("AVANCE", ConsumptionStatus.Aprobado)], 1, 20, 1)));

            var handler = new GetCreditCardByIdQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCreditCardByIdQuery { Id = 1 }, CancellationToken.None);

            result.Consumptions.Single().CommerceName.Should().Be("AVANCE");
        }

        [Fact]
        public async Task GetCreditCardById_WithUnknownCard_ShouldReportItAsNotFound()
        {
            _creditCardsServices
                .Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<CreditCardDto>.Failure(CreditCardError.NonExistsCreditCard));

            var handler = new GetCreditCardByIdQueryHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(
                new GetCreditCardByIdQuery { Id = 999 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region Asignación

        [Fact]
        public async Task CreateCreditCard_ShouldReturnTheAssignedCard()
        {
            _creditCardsServices
                .Setup(service => service.AssignCreditCardAsync(It.IsAny<CreditCardAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Success());

            GivenPagedCards(BuildCardDto());

            var handler = new CreateCreditCardCommandHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(
                new CreateCreditCardCommand { ClientId = ClientId, CreditLimit = 50_000m },
                CancellationToken.None);

            result.LastFourDigits.Should().Be("1234");
            result.CreditLimit.Should().Be(50_000m);
        }

        [Fact]
        public async Task CreateCreditCard_WithInactiveClient_ShouldRejectTheRequest()
        {
            _creditCardsServices
                .Setup(service => service.AssignCreditCardAsync(It.IsAny<CreditCardAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(CreditCardError.CustomerIsNotActive));

            var handler = new CreateCreditCardCommandHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(
                new CreateCreditCardCommand { ClientId = ClientId, CreditLimit = 50_000m },
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        //Agotar los reintentos del número de 16 dígitos es un conflicto.
        [Fact]
        public async Task CreateCreditCard_WhenTheNumberCannotBeIssued_ShouldReportItAsConflict()
        {
            _creditCardsServices
                .Setup(service => service.AssignCreditCardAsync(It.IsAny<CreditCardAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(CreditCardError.FailedGenerateCardNumber));

            var handler = new CreateCreditCardCommandHandler(
                _creditCardsServices.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(
                new CreateCreditCardCommand { ClientId = ClientId, CreditLimit = 50_000m },
                CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region Límite y cancelación

        [Fact]
        public async Task UpdateCreditCardLimit_ShouldForwardTheNewLimit()
        {
            _creditCardsServices
                .Setup(service => service.EditCreditCardLimitAsync(It.IsAny<EditCardLimitDto>()))
                .ReturnsAsync(ValidationResult<CardLimitUpdatedDto>.Success(new CardLimitUpdatedDto
                {
                    CustomerId = ClientId,
                    LastFourDigits = "1234",
                    CreditLimit = 75_000m,
                    ModifiedAt = DateTimeOffset.UtcNow
                }));

            var handler = new UpdateCreditCardLimitCommandHandler(_creditCardsServices.Object);

            await handler.Handle(
                new UpdateCreditCardLimitCommand { Id = 1, CreditLimit = 75_000m }, CancellationToken.None);

            _creditCardsServices.Verify(service => service.EditCreditCardLimitAsync(
                It.Is<EditCardLimitDto>(dto => dto.Id == 1 && dto.CreditLimit == 75_000m)), Times.Once);
        }

        //El nuevo límite no puede quedar por debajo de la deuda actual.
        [Fact]
        public async Task UpdateCreditCardLimit_BelowTheCurrentDebt_ShouldRejectTheRequest()
        {
            _creditCardsServices
                .Setup(service => service.EditCreditCardLimitAsync(It.IsAny<EditCardLimitDto>()))
                .ReturnsAsync(ValidationResult<CardLimitUpdatedDto>.Failure(
                    CreditCardError.CreditLimitLowerThanOwedAmount));

            var handler = new UpdateCreditCardLimitCommandHandler(_creditCardsServices.Object);

            var act = async () => await handler.Handle(
                new UpdateCreditCardLimitCommand { Id = 1, CreditLimit = 100m }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        //Mensaje literal del documento para la cancelación con deuda pendiente.
        [Fact]
        public async Task CancelCreditCard_WithPendingDebt_ShouldRejectItWithTheDocumentedMessage()
        {
            _creditCardsServices
                .Setup(service => service.CancelCreditCardAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult.Failure(CreditCardError.CreditCardWithPendingDebt));

            var handler = new CancelCreditCardCommandHandler(_creditCardsServices.Object);

            var act = async () => await handler.Handle(
                new CancelCreditCardCommand { Id = 1 }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(CreditCardError.CreditCardWithPendingDebt.Description);
        }

        [Fact]
        public async Task CancelCreditCard_WithoutDebt_ShouldCancelTheCard()
        {
            _creditCardsServices
                .Setup(service => service.CancelCreditCardAsync(1))
                .ReturnsAsync(ValidationResult.Success());

            var handler = new CancelCreditCardCommandHandler(_creditCardsServices.Object);

            await handler.Handle(new CancelCreditCardCommand { Id = 1 }, CancellationToken.None);

            _creditCardsServices.Verify(service => service.CancelCreditCardAsync(1), Times.Once);
        }

        [Fact]
        public async Task CancelCreditCard_WithUnknownCard_ShouldReportItAsNotFound()
        {
            _creditCardsServices
                .Setup(service => service.CancelCreditCardAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult.Failure(CreditCardError.NonExistsCreditCard));

            var handler = new CancelCreditCardCommandHandler(_creditCardsServices.Object);

            var act = async () => await handler.Handle(
                new CancelCreditCardCommand { Id = 999 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region builders

        private void GivenPagedCards(params CreditCardDto[] cards)
            => _creditCardsServices
                .Setup(service => service.GetPagedCreditCardsAsync(It.IsAny<CreditCardFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<CreditCardDto>>.Success(
                    new PagedResult<CreditCardDto>(cards, 1, 20, cards.Length)));

        private static CreditCardDto BuildCardDto()
            => new()
            {
                Id = 1,
                MaskedCardNumber = "************1234",
                LastFourDigits = "1234",
                CustomerId = ClientId,
                FullNameCustomer = "Maria Gomez",
                CreditLimit = 50_000m,
                ExpirationDate = "03/29",
                OwedAmount = 15_000m,
                AvailableCredit = 35_000m,
                StateCreditCard = CreditCardStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow
            };

        private static CardConsumptionDto BuildConsumption(string commerceName, ConsumptionStatus status)
            => new()
            {
                ConsumptionDate = DateTimeOffset.UtcNow,
                Amount = 2_500m,
                CommerceName = commerceName,
                StateConsumption = status
            };

        #endregion
    }
}
