using Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.ViewModels.AdminDashboard;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.AdminDashboard
{
    public sealed class AdminDashboardMappingTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;

        public AdminDashboardMappingTests()
        {
            _configuration = new MapperConfiguration(
                configuration => configuration.AddProfile<AdminDashboardMappingDtoToViewModel>(),
                NullLoggerFactory.Instance);

            _mapper = _configuration.CreateMapper();
        }

        //El mapeo va por convención de nombres: si un indicador se renombra en un solo lado,
        //esta prueba lo detecta antes de que la vista muestre un cero silencioso.
        [Fact]
        public void Configuration_ShouldBeValid()
            => _configuration.AssertConfigurationIsValid();

        [Fact]
        public void AdminDashboardDtoToViewModel_ShouldCarryTheElevenIndicators()
        {
            var dto = new AdminDashboardDto
            {
                TotalHistoricalTransactions = 140,
                DayTransactions = 7,
                TotalHistoricalPay = 30,
                DayPay = 2,
                CustomerActive = 12,
                CustomerInactive = 3,
                TotalFinancialProducts = 21,
                OutstandingLoans = 4,
                CreditCardActive = 6,
                SavingAccountActive = 11,
                AverageDebtAmountPerCustomer = 7500.55m
            };

            var viewModel = _mapper.Map<AdminDashboardViewModel>(dto);

            viewModel.TotalHistoricalTransactions.Should().Be(140);
            viewModel.DayTransactions.Should().Be(7);
            viewModel.TotalHistoricalPay.Should().Be(30);
            viewModel.DayPay.Should().Be(2);
            viewModel.CustomerActive.Should().Be(12);
            viewModel.CustomerInactive.Should().Be(3);
            viewModel.TotalFinancialProducts.Should().Be(21);
            viewModel.OutstandingLoans.Should().Be(4);
            viewModel.CreditCardActive.Should().Be(6);
            viewModel.SavingAccountActive.Should().Be(11);

            //El promedio es un monto: los decimales no se pueden perder por el camino
            viewModel.AverageDebtAmountPerCustomer.Should().Be(7500.55m);
        }
    }
}
