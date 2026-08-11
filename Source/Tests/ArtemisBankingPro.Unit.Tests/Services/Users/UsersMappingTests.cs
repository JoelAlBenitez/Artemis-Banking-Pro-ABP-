using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Users;
using Artemis_Banking_Pro.Core.Application.ViewModels.Users;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //Mapeos del mantenimiento de usuarios: las pantallas solo reciben ViewModels, así que un
    //miembro sin mapear deja un campo vacío en la pantalla sin fallar en tiempo de compilación.
    public sealed class UsersMappingTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;

        public UsersMappingTests()
        {
            _configuration = new MapperConfiguration(
                configuration => configuration.AddProfile<UsersMappingDtoToViewModelAndReverse>(),
                NullLoggerFactory.Instance);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void Configuration_ShouldBeValid()
            => _configuration.AssertConfigurationIsValid();

        [Fact]
        public void UserDtoToViewModel_ShouldBuildTheFullNameAndTheStateLabel()
        {
            var dto = new UserDto
            {
                IdUser = "user-1",
                Name = "María",
                LastName = "Gómez",
                UserName = "cliente01",
                Email = "cliente01@artemisbank.com",
                IDCARD = "00100000009",
                State = true,
                TypeUser = Roles.Cliente
            };

            var viewModel = _mapper.Map<UserViewModel>(dto);

            viewModel.Id.Should().Be("user-1");
            viewModel.FullName.Should().Be("María Gómez");
            viewModel.State.Should().Be("Activo");
            viewModel.IsActive.Should().BeTrue();
            viewModel.TypeUser.Should().Be(nameof(Roles.Cliente));
        }

        [Fact]
        public void UserDtoToViewModel_WithAnInactiveUser_ShouldShowTheInactiveLabel()
        {
            var dto = new UserDto
            {
                IdUser = "user-2",
                Name = "Juan",
                LastName = "Pérez",
                UserName = "cajero01",
                Email = "cajero01@artemisbank.com",
                IDCARD = "00100000010",
                State = false,
                TypeUser = Roles.Cajero
            };

            var viewModel = _mapper.Map<UserViewModel>(dto);

            viewModel.State.Should().Be("Inactivo");
            viewModel.IsActive.Should().BeFalse();
        }

        //La contraseña y el monto adicional se piden en blanco en cada carga de la edición
        [Fact]
        public void UserDetailDtoToEditViewModel_ShouldNotPreloadThePasswordNorTheAmount()
        {
            var dto = new UserDetailDto
            {
                Id = "user-1",
                UserName = "cliente01",
                Name = "María",
                LastName = "Gómez",
                IDCARD = "00100000009",
                Email = "cliente01@artemisbank.com",
                TypeUser = Roles.Cliente,
                State = true,
                IsClient = true
            };

            var viewModel = _mapper.Map<EditUserViewModel>(dto);

            viewModel.Id.Should().Be("user-1");
            viewModel.Name.Should().Be("María");
            viewModel.IsClient.Should().BeTrue();
            viewModel.NewPassword.Should().BeNull();
            viewModel.ConfirmNewPassword.Should().BeNull();
            viewModel.AdditionalAmount.Should().BeNull();
        }

        [Fact]
        public void EditViewModelToDto_ShouldCarryTheEditableData()
        {
            var viewModel = new EditUserViewModel
            {
                Id = "user-1",
                Name = "María",
                LastName = "Gómez",
                IDCARD = "00100000009",
                Email = "cliente01@artemisbank.com",
                UserName = "cliente01",
                NewPassword = "Nueva123!",
                ConfirmNewPassword = "Nueva123!",
                AdditionalAmount = 500m,
                IsClient = true
            };

            var dto = _mapper.Map<EditUserDto>(viewModel);

            dto.Id.Should().Be("user-1");
            dto.UserName.Should().Be("cliente01");
            dto.NewPassword.Should().Be("Nueva123!");
            dto.ConfirmNewPassword.Should().Be("Nueva123!");
            dto.AdditionalAmount.Should().Be(500m);
        }

        //El origen del enlace de activación lo resuelve el controlador, no el mapeo
        [Fact]
        public void SaveViewModelToRegisterRequest_ShouldNotSetTheOrigin()
        {
            var viewModel = new SaveUserViewModel
            {
                FirstName = "María",
                LastName = "Gómez",
                IDCARD = "00100000009",
                Email = "cliente01@artemisbank.com",
                UserName = "cliente01",
                Password = "Clave123!",
                ConfirmPassword = "Clave123!",
                Role = nameof(Roles.Cliente),
                InitialAmount = 1000m
            };

            var request = _mapper.Map<RegisterRequest>(viewModel);

            request.FirstName.Should().Be("María");
            request.Role.Should().Be(nameof(Roles.Cliente));
            request.InitialAmount.Should().Be(1000m);
            request.Origin.Should().BeNull();
        }
    }
}
