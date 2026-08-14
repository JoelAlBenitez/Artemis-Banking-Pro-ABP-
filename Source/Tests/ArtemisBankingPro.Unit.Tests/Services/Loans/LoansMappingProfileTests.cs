using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.ViewModels.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Loans
{
    public sealed class LoansMappingProfileTests
    {
        private readonly MapperConfiguration _configuration;
        private readonly IMapper _mapper;

        public LoansMappingProfileTests()
        {
            _configuration = new MapperConfiguration(
                configuration =>
                {
                    configuration.AddProfile<LoansMappingEntitieToDtoAndReverse>();
                    configuration.AddProfile<LoansMappingDtoToViewModelAndReverse>();
                },
                NullLoggerFactory.Instance);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void LoansProfiles_ShouldBeValid()
        {
            var validation = () => _configuration.AssertConfigurationIsValid();

            validation.Should().NotThrow();
        }

        [Fact]
        public void RiskWarningViewModel_ShouldReceiveTheLoanDataAndTheRiskDataOverTheSameModel()
        {
            var assignment = new LoansAssignmentDto
            {
                CustomerId = "customer-1",
                TermLoans = TermMonths.Meses12,
                AmmountLoans = 100_000m,
                AnnualInterestRate = 12m
            };

            var evaluation = new LoanRiskEvaluationDto
            {
                RiskType = LoanRiskType.DeudaProyectada,
                Message = "mensaje de advertencia",
                CurrentDebt = 25_000m,
                ProjectedDebt = 132_500m,
                AverageDebt = 80_000m
            };

            var warning = _mapper.Map<RiskWarningViewModel>(assignment);
            _mapper.Map(evaluation, warning);

            warning.CustomerId.Should().Be("customer-1");
            warning.AmmountLoans.Should().Be(100_000m);
            warning.Message.Should().Be("mensaje de advertencia");
            warning.ProjectedDebt.Should().Be(132_500m);
            warning.AverageDebt.Should().Be(80_000m);
        }

        [Fact]
        public void RiskWarningViewModel_ShouldReturnToTheAssignmentDtoWhenTheAdminConfirms()
        {
            var warning = new RiskWarningViewModel
            {
                CustomerId = "customer-1",
                TermLoans = TermMonths.Meses24,
                AmmountLoans = 50_000m,
                AnnualInterestRate = 18m,
                Message = "mensaje",
                CurrentDebt = 1m,
                ProjectedDebt = 2m,
                AverageDebt = 3m
            };

            var dto = _mapper.Map<LoansAssignmentDto>(warning);

            dto.CustomerId.Should().Be("customer-1");
            dto.TermLoans.Should().Be(TermMonths.Meses24);
            dto.AmmountLoans.Should().Be(50_000m);
            dto.AnnualInterestRate.Should().Be(18m);
            dto.ConfirmHighRisk.Should().BeFalse();
        }

        [Fact]
        public void ClientsForLoanAssignment_ShouldProjectTheAverageDebtAndTheEligibleClients()
        {
            var dto = new ClientsForLoanAssignmentDto
            {
                AverageDebt = 80_000m,
                Clients = new[]
                {
                    new ClientLoansDto
                    {
                        Id = "customer-1",
                        IdCard = "40200000001",
                        FullName = "María Gómez",
                        Email = "maria@artemis.com",
                        TotalDebtAmount = 25_000m
                    }
                }
            };

            var vm = _mapper.Map<ClientsForLoanAssignmentViewModel>(dto);

            vm.AverageDebt.Should().Be(80_000m);
            vm.Clients.Should().ContainSingle();
            vm.Clients.Single().TotalDebtAmount.Should().Be(25_000m);
        }
    }
}
