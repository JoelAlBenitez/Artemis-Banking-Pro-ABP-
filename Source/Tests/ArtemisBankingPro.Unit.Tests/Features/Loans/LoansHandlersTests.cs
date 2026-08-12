using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.CreateLoan;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.UpdateLoanRate;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetAllLoans;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetLoanById;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Loans
{
    public sealed class LoansHandlersTests
    {
        private const string ClientId = "cliente-1";

        private readonly Mock<ILoansServices> _loansServices = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();

        #region Listado

        [Fact]
        public async Task GetAllLoans_ShouldProjectTheDocumentedShape()
        {
            _loansServices
                .Setup(service => service.GetPagedLoansAsync(It.IsAny<LoansFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<LoansDto>>.Success(
                    new PagedResult<LoansDto>([BuildLoan()], 1, 20, 1)));

            var handler = new GetAllLoansQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllLoansQuery(), CancellationToken.None);

            var loan = result.Data.Single();
            loan.LoanNumber.Should().Be("987654321");
            loan.CapitalAmount.Should().Be(100_000m);
            loan.TermInMonths.Should().Be(12);
            loan.Status.Should().Be(nameof(LoanStatus.Activo));
            loan.ClientPaymentStatus.Should().Be("Al día");
        }

        [Fact]
        public async Task GetAllLoans_WithClientInArrears_ShouldReportItAsOverdue()
        {
            var loan = BuildLoan();
            loan.CustomerInArrears = true;

            _loansServices
                .Setup(service => service.GetPagedLoansAsync(It.IsAny<LoansFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<LoansDto>>.Success(
                    new PagedResult<LoansDto>([loan], 1, 20, 1)));

            var handler = new GetAllLoansQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllLoansQuery(), CancellationToken.None);

            result.Data.Single().ClientPaymentStatus.Should().Be("En mora");
        }

        //Buscar por una cédula sin cliente registrado es un recurso inexistente, no un 400.
        [Fact]
        public async Task GetAllLoans_WithUnknownIdentification_ShouldReturnAnEmptyPage()
        {
            _loansServices
                .Setup(service => service.GetPagedLoansAsync(It.IsAny<LoansFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<LoansDto>>.Failure(
                    LoansError.NonExistsCustomerByIdCard));

            var handler = new GetAllLoansQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(
                new GetAllLoansQuery { Identification = "00000000000" }, CancellationToken.None);

            result.Data.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.Page.Should().Be(1);
        }

        [Fact]
        public async Task GetAllLoans_ShouldForwardTheStatusFilter()
        {
            _loansServices
                .Setup(service => service.GetPagedLoansAsync(It.IsAny<LoansFilterDto>()))
                .ReturnsAsync(ValidationResult<PagedResult<LoansDto>>.Success(
                    new PagedResult<LoansDto>([], 1, 20, 0)));

            var handler = new GetAllLoansQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            await handler.Handle(new GetAllLoansQuery { Status = "completados" }, CancellationToken.None);

            _loansServices.Verify(service => service.GetPagedLoansAsync(
                It.Is<LoansFilterDto>(filter => filter.Status == LoanStatusFilter.Completados)), Times.Once);
        }

        #endregion

        #region Detalle

        //La cuota mensual, el pendiente y el estado de pago se derivan de la amortización.
        [Fact]
        public async Task GetLoanById_ShouldDeriveTheInstallmentAndThePendingAmount()
        {
            _loansServices
                .Setup(service => service.GetDetailLoanAsync(1))
                .ReturnsAsync(ValidationResult<DetailLoansDto>.Success(BuildDetail()));

            var handler = new GetLoanByIdQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetLoanByIdQuery { Id = 1 }, CancellationToken.None);

            result.MonthlyInstallment.Should().Be(8_884.88m);
            result.PendingAmount.Should().Be(8_884.88m);
            result.ClientPaymentStatus.Should().Be("Al día");
            result.Amortization.Should().HaveCount(2);
        }

        //El desglose de capital e interés es parte del contrato del detalle.
        [Fact]
        public async Task GetLoanById_ShouldExposeTheInterestAndCapitalOfEachInstallment()
        {
            _loansServices
                .Setup(service => service.GetDetailLoanAsync(1))
                .ReturnsAsync(ValidationResult<DetailLoansDto>.Success(BuildDetail()));

            var handler = new GetLoanByIdQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetLoanByIdQuery { Id = 1 }, CancellationToken.None);

            var first = result.Amortization.First();
            first.InterestAmount.Should().Be(1_000m);
            first.CapitalAmount.Should().Be(7_884.88m);
        }

        [Fact]
        public async Task GetLoanById_WithOverdueInstallment_ShouldReportTheClientAsOverdue()
        {
            var detail = BuildDetail();
            detail.loansInstallmentDtos[1].IsOverdue = true;

            _loansServices
                .Setup(service => service.GetDetailLoanAsync(1))
                .ReturnsAsync(ValidationResult<DetailLoansDto>.Success(detail));

            var handler = new GetLoanByIdQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetLoanByIdQuery { Id = 1 }, CancellationToken.None);

            result.ClientPaymentStatus.Should().Be("En mora");
        }

        [Fact]
        public async Task GetLoanById_WithUnknownLoan_ShouldReportItAsNotFound()
        {
            _loansServices
                .Setup(service => service.GetDetailLoanAsync(It.IsAny<int>()))
                .ReturnsAsync(ValidationResult<DetailLoansDto>.Failure(LoansError.NonExistsLoan));

            var handler = new GetLoanByIdQueryHandler(_loansServices.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(new GetLoanByIdQuery { Id = 999 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region Asignación y alto riesgo

        //Sin confirmación el préstamo no se crea: la respuesta lleva los montos que el
        //administrador necesita para decidir.
        [Fact]
        public async Task CreateLoan_WithHighRiskAndWithoutConfirmation_ShouldReturnTheConflictWithoutCreating()
        {
            _loansServices
                .Setup(service => service.EvaluateRiskAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult<LoanRiskEvaluationDto>.Success(BuildRisk(LoanRiskType.DeudaProyectada)));

            var handler = new CreateLoanCommandHandler(_loansServices.Object, _userManagementService.Object);

            var result = await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            result.Loan.Should().BeNull();
            result.HighRisk.Should().NotBeNull();
            result.HighRisk!.CurrentDebt.Should().Be(25_000m);
            result.HighRisk.ProjectedDebt.Should().Be(132_500m);
            result.HighRisk.AverageDebt.Should().Be(80_000m);

            _loansServices.Verify(service => service.CreateAsync(It.IsAny<LoansAssignmentDto>()), Times.Never);
        }

        //El documento fija el valor de riskType; el nombre del enum del dominio está en español.
        [Theory]
        [InlineData(LoanRiskType.DeudaProyectada, "ProjectedHighRisk")]
        [InlineData(LoanRiskType.DeudaActual, "CurrentHighRisk")]
        public async Task CreateLoan_WithHighRisk_ShouldUseTheDocumentedRiskType(
            LoanRiskType riskType, string expected)
        {
            _loansServices
                .Setup(service => service.EvaluateRiskAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult<LoanRiskEvaluationDto>.Success(BuildRisk(riskType)));

            var handler = new CreateLoanCommandHandler(_loansServices.Object, _userManagementService.Object);

            var result = await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            result.HighRisk!.RiskType.Should().Be(expected);
        }

        [Fact]
        public async Task CreateLoan_WithHighRiskConfirmed_ShouldCreateTheLoan()
        {
            _loansServices
                .Setup(service => service.EvaluateRiskAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult<LoanRiskEvaluationDto>.Success(BuildRisk(LoanRiskType.DeudaProyectada)));

            _loansServices
                .Setup(service => service.CreateAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Success());

            _loansServices
                .Setup(service => service.GetActiveLoansByCustomerAsync(ClientId))
                .ReturnsAsync(ValidationResult<IReadOnlyCollection<LoansDto>>.Success([BuildLoan()]));

            _loansServices
                .Setup(service => service.GetDetailLoanAsync(1))
                .ReturnsAsync(ValidationResult<DetailLoansDto>.Success(BuildDetail()));

            _userManagementService
                .Setup(service => service.GetFullNameByIdAsync(ClientId))
                .ReturnsAsync("Maria Gomez");

            var handler = new CreateLoanCommandHandler(_loansServices.Object, _userManagementService.Object);

            var command = BuildCreateCommand();
            command.ConfirmHighRisk = true;

            var result = await handler.Handle(command, CancellationToken.None);

            result.HighRisk.Should().BeNull();
            result.Loan.Should().NotBeNull();
            result.Loan!.LoanNumber.Should().Be("987654321");
            result.Loan.MonthlyInstallment.Should().Be(8_884.88m);
            //Total a pagar: la suma de todas las cuotas de la tabla de amortización
            result.Loan.TotalAmountToPay.Should().Be(17_769.76m);
            result.Loan.ClientFullName.Should().Be("Maria Gomez");
        }

        //Un cliente solo puede tener un préstamo activo a la vez.
        [Fact]
        public async Task CreateLoan_WhenTheClientAlreadyHasAnActiveLoan_ShouldReportItAsConflict()
        {
            _loansServices
                .Setup(service => service.EvaluateRiskAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult<LoanRiskEvaluationDto>.Success(BuildRisk(LoanRiskType.SinRiesgo)));

            _loansServices
                .Setup(service => service.CreateAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult.Failure(LoansError.CustomerWithLoanExist));

            var handler = new CreateLoanCommandHandler(_loansServices.Object, _userManagementService.Object);

            var act = async () => await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("Este cliente ya tiene un préstamo activo asignado.");
        }

        [Fact]
        public async Task CreateLoan_ShouldForwardTheTermAsTheDomainEnum()
        {
            _loansServices
                .Setup(service => service.EvaluateRiskAsync(It.IsAny<LoansAssignmentDto>()))
                .ReturnsAsync(ValidationResult<LoanRiskEvaluationDto>.Success(BuildRisk(LoanRiskType.DeudaProyectada)));

            var handler = new CreateLoanCommandHandler(_loansServices.Object, _userManagementService.Object);

            await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            _loansServices.Verify(service => service.EvaluateRiskAsync(
                It.Is<LoansAssignmentDto>(dto => dto.TermLoans == TermMonths.Meses12)), Times.Once);
        }

        #endregion

        #region Tasa de interés

        [Fact]
        public async Task UpdateLoanRate_ShouldForwardTheNewRate()
        {
            _loansServices
                .Setup(service => service.EditAnnualInterestRateAsync(It.IsAny<EditAnnualInterestRateDto>()))
                .ReturnsAsync(ValidationResult.Success());

            var handler = new UpdateLoanRateCommandHandler(_loansServices.Object);

            await handler.Handle(
                new UpdateLoanRateCommand { Id = 1, AnnualInterestRate = 10.5m }, CancellationToken.None);

            _loansServices.Verify(service => service.EditAnnualInterestRateAsync(
                It.Is<EditAnnualInterestRateDto>(dto => dto.Id == 1 && dto.AnnualInterestRate == 10.5m)),
                Times.Once);
        }

        //Sin cuotas futuras pendientes no hay nada que recalcular: el documento lo responde 400.
        [Fact]
        public async Task UpdateLoanRate_WithoutPendingFutureInstallments_ShouldRejectTheRequest()
        {
            _loansServices
                .Setup(service => service.EditAnnualInterestRateAsync(It.IsAny<EditAnnualInterestRateDto>()))
                .ReturnsAsync(ValidationResult.Failure(LoansError.NonExistsFutureInstallments));

            var handler = new UpdateLoanRateCommandHandler(_loansServices.Object);

            var act = async () => await handler.Handle(
                new UpdateLoanRateCommand { Id = 1, AnnualInterestRate = 10.5m }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("No existen cuotas futuras pendientes para recalcular.");
        }

        [Fact]
        public async Task UpdateLoanRate_WithUnknownLoan_ShouldReportItAsNotFound()
        {
            _loansServices
                .Setup(service => service.EditAnnualInterestRateAsync(It.IsAny<EditAnnualInterestRateDto>()))
                .ReturnsAsync(ValidationResult.Failure(LoansError.NonExistsLoan));

            var handler = new UpdateLoanRateCommandHandler(_loansServices.Object);

            var act = async () => await handler.Handle(
                new UpdateLoanRateCommand { Id = 999, AnnualInterestRate = 10.5m }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region builders

        private static CreateLoanCommand BuildCreateCommand()
            => new()
            {
                ClientId = ClientId,
                CapitalAmount = 100_000m,
                TermInMonths = 12,
                AnnualInterestRate = 12m
            };

        private static LoanRiskEvaluationDto BuildRisk(LoanRiskType riskType)
            => new()
            {
                RiskType = riskType,
                Message = "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, " +
                          "ya que su deuda superará el umbral promedio del sistema.",
                CurrentDebt = 25_000m,
                ProjectedDebt = 132_500m,
                AverageDebt = 80_000m
            };

        private static LoansDto BuildLoan()
            => new()
            {
                Id = 1,
                LoanNumber = "987654321",
                CustomerId = ClientId,
                FullNameCustomer = "Maria Gomez",
                AprovechedCapital = 100_000m,
                QuantityInstallment = 12,
                InstallmentPay = 3,
                PendientAmount = 76_250m,
                AnnualInterestRate = 12m,
                Term = 12,
                StateLoans = LoanStatus.Activo,
                CustomerInArrears = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

        private static DetailLoansDto BuildDetail()
            => new()
            {
                Id = 1,
                NumberLoand = "987654321",
                CustomerId = ClientId,
                FullNameCustomer = "Maria Gomez",
                ApprovedAmount = 100_000m,
                AnnualInterestRate = 12m,
                Term = 12,
                StateLoans = LoanStatus.Activo,
                loansInstallmentDtos =
                [
                    new LoansInstallmentDto
                    {
                        NumberLoanInstallment = 1,
                        DueDate = DateTimeOffset.UtcNow.AddMonths(1),
                        InstallmentValue = 8_884.88m,
                        InterestAmount = 1_000m,
                        CapitalAmount = 7_884.88m,
                        OutstandingBalance = 0m,
                        StateInstallment = PaymentStatus.Pagada,
                        IsOverdue = false
                    },
                    new LoansInstallmentDto
                    {
                        NumberLoanInstallment = 2,
                        DueDate = DateTimeOffset.UtcNow.AddMonths(2),
                        InstallmentValue = 8_884.88m,
                        InterestAmount = 921.15m,
                        CapitalAmount = 7_963.73m,
                        OutstandingBalance = 8_884.88m,
                        StateInstallment = PaymentStatus.Pendiente,
                        IsOverdue = false
                    }
                ]
            };

        #endregion
    }
}
