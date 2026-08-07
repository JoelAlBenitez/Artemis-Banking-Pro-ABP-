using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.SavingsAccounts
{
    public sealed class SavingsAccountsMappingTests
    {
        private readonly MapperConfiguration _configuration;
        private readonly IMapper _mapper;

        public SavingsAccountsMappingTests()
        {
            _configuration = new MapperConfiguration(
                configuration =>
                {
                    configuration.AddProfile<SavingsAccountsMappingEntitieToDtoAndReverse>();
                    configuration.AddProfile<SavingsAccountsMappingDtoToViewModelAndReverse>();
                },
                NullLoggerFactory.Instance);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void SavingsAccountsProfiles_ShouldBeValid()
        {
            var validation = () => _configuration.AssertConfigurationIsValid();

            validation.Should().NotThrow();
        }

        //Reproduce el escaneo de assembly que hace ApplicationDependencies al arrancar la WebApp:
        //dos Profile que declaren el mismo par de tipos rompen la construcción del contenedor.
        [Fact]
        public void ApplicationProfiles_ShouldNotDeclareDuplicatedTypeMaps()
        {
            var build = () =>
            {
                var configuration = new MapperConfiguration(
                    expression => expression.AddMaps(
                        typeof(SavingsAccountsMappingEntitieToDtoAndReverse).Assembly),
                    NullLoggerFactory.Instance);

                configuration.CreateMapper();
            };

            build.Should().NotThrow();
        }

        [Fact]
        public void SavingsAccount_ShouldProjectTypeAndStatusOverTheDto()
        {
            var savingsAccount = new SavingsAccount
            {
                Id = 7,
                AccountNumber = "100000001",
                CustomerId = "customer-1",
                Balance = 1_500m,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

            var dto = _mapper.Map<SavingsAccountDto>(savingsAccount);

            dto.Id.Should().Be(7);
            dto.AccountNumber.Should().Be("100000001");
            dto.Balance.Should().Be(1_500m);
            dto.TypeSavingsAccount.Should().Be(SavingsAccountType.Secundaria);
            dto.StateSavingsAccount.Should().Be(SavingsAccountStatus.Activa);

            //Provienen de Identity: el perfil los ignora y los completa el servicio
            dto.FullNameCustomer.Should().BeNull();
            dto.IdCard.Should().BeNull();
        }

        //El Dashboard del módulo Cliente materializa entidades a partir del DTO.
        [Fact]
        public void SavingsAccountDto_ShouldReturnToTheEntityWithItsTypeAndStatus()
        {
            var dto = new SavingsAccountDto
            {
                Id = 7,
                AccountNumber = "100000001",
                CustomerId = "customer-1",
                FullNameCustomer = "María Gómez",
                IdCard = "40200000001",
                Balance = 1_500m,
                TypeSavingsAccount = SavingsAccountType.Principal,
                StateSavingsAccount = SavingsAccountStatus.Cancelada
            };

            var entity = _mapper.Map<SavingsAccount>(dto);

            entity.AccountNumber.Should().Be("100000001");
            entity.CustomerId.Should().Be("customer-1");
            entity.Balance.Should().Be(1_500m);
            entity.AccountType.Should().Be(SavingsAccountType.Principal);
            entity.Status.Should().Be(SavingsAccountStatus.Cancelada);
        }

        [Fact]
        public void Assignment_ShouldAlwaysProduceASecondaryAndActiveAccount()
        {
            var dto = new SavingsAccountAssignmentDto
            {
                CustomerId = "customer-1",
                InitialBalance = 2_000m
            };

            var entity = _mapper.Map<SavingsAccount>(dto);

            entity.CustomerId.Should().Be("customer-1");
            entity.Balance.Should().Be(2_000m);
            entity.AccountType.Should().Be(SavingsAccountType.Secundaria);
            entity.Status.Should().Be(SavingsAccountStatus.Activa);
            entity.AccountNumber.Should().BeNull();
        }

        [Theory]
        [InlineData(SavingsAccountType.Secundaria, SavingsAccountStatus.Activa, true)]
        [InlineData(SavingsAccountType.Secundaria, SavingsAccountStatus.Cancelada, false)]
        [InlineData(SavingsAccountType.Principal, SavingsAccountStatus.Activa, false)]
        public void SavingsAccountViewModel_ShouldOnlyOfferCancelForActiveSecondaryAccounts(
            SavingsAccountType type, SavingsAccountStatus status, bool expected)
        {
            var dto = new SavingsAccountDto
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = "customer-1",
                FullNameCustomer = "María Gómez",
                IdCard = "40200000001",
                Balance = 0m,
                TypeSavingsAccount = type,
                StateSavingsAccount = status
            };

            var viewModel = _mapper.Map<SavingsAccountViewModel>(dto);

            viewModel.CanBeCancelled.Should().Be(expected);
        }

        [Fact]
        public void TransactionViewModel_ShouldTranslateTheTypeAndStatusToTheLabelsOfTheDetail()
        {
            var dto = new TransactionDto
            {
                TransactionDate = DateTimeOffset.UtcNow,
                Amount = 500m,
                TypeTransaction = TransactionType.Debito,
                Beneficiary = "100000002",
                Origin = "100000001",
                StateTransaction = TransactionStatus.Aprobada
            };

            var viewModel = _mapper.Map<TransactionViewModel>(dto);

            viewModel.TypeTransaction.Should().Be("DÉBITO");
            viewModel.StateTransaction.Should().Be("APROBADA");
            viewModel.Amount.Should().Be(500m);
        }
    }
}
