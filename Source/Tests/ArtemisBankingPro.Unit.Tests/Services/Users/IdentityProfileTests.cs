using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Infraestructrue.Identity.Mappings;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //Los mapeos de Identity: si un miembro del DTO deja de tener origen, la validación de
    //configuración falla aquí y no en tiempo de ejecución.
    public sealed class IdentityProfileTests
    {
        private readonly IMapper _mapper;

        public IdentityProfileTests()
        {
            var configuration = new MapperConfiguration(
                config => config.AddProfile<IdentityProfile>(),
                NullLoggerFactory.Instance);

            configuration.AssertConfigurationIsValid();
            _mapper = configuration.CreateMapper();
        }

        [Fact]
        public void ApplicationUserToUserDto_ShouldCarryTheFieldsRequiredByTheContract()
        {
            var user = IdentityMocks.BuildUser();

            var dto = _mapper.Map<UserDto>(user);

            dto.IdUser.Should().Be(user.Id);
            dto.UserName.Should().Be(user.UserName);
            dto.Name.Should().Be(user.FirstName);
            dto.LastName.Should().Be(user.LastName);
            dto.IDCARD.Should().Be(user.IDCARD);
            dto.Email.Should().Be(user.Email);
            dto.State.Should().BeTrue();
        }

        [Fact]
        public void ApplicationUserToUserDto_WithAnInactiveUser_ShouldReportTheStateAsFalse()
        {
            var dto = _mapper.Map<UserDto>(IdentityMocks.BuildUser(isActive: false));

            dto.State.Should().BeFalse();
        }

        //El cajero necesita el titular en una sola cadena: nombre y apellido concatenados
        [Fact]
        public void ApplicationUserToClientSummaryDto_ShouldComposeTheFullName()
        {
            var user = IdentityMocks.BuildUser();

            var dto = _mapper.Map<ClientSummaryDto>(user);

            dto.FullName.Should().Be("María Gómez");
            dto.IDCARD.Should().Be(user.IDCARD);
            dto.Email.Should().Be(user.Email);
        }

        [Fact]
        public void ApplicationUserToClientBaseDataDto_ShouldReturnIdNameAndLastName()
        {
            var user = IdentityMocks.BuildUser();

            var dto = _mapper.Map<ClientBaseDataDto>(user);

            dto.Id.Should().Be(user.Id);
            dto.FirstName.Should().Be(user.FirstName);
            dto.LastName.Should().Be(user.LastName);
        }

        [Fact]
        public void ApplicationUserToUserDetailDto_ShouldCarryTheStateAndTheEditableFields()
        {
            var user = IdentityMocks.BuildUser();

            var dto = _mapper.Map<UserDetailDto>(user);

            dto.Id.Should().Be(user.Id);
            dto.UserName.Should().Be(user.UserName);
            dto.Name.Should().Be(user.FirstName);
            dto.State.Should().BeTrue();
        }

        //El formulario de edición nunca precarga la contraseña ni el monto adicional
        [Fact]
        public void ApplicationUserToEditUserDto_ShouldNotPreloadThePasswordOrTheAdditionalAmount()
        {
            var dto = _mapper.Map<EditUserDto>(IdentityMocks.BuildUser());

            dto.NewPassword.Should().BeNull();
            dto.ConfirmNewPassword.Should().BeNull();
            dto.AdditionalAmount.Should().BeNull();
            dto.UserName.Should().Be("usuario01");
        }

        [Theory]
        [InlineData(0, 20, 0)]
        [InlineData(1, 20, 1)]
        [InlineData(20, 20, 1)]
        [InlineData(21, 20, 2)]
        [InlineData(41, 20, 3)]
        public void PagedResponseDto_ShouldCalculateTheTotalPagesFromTheAppliedPageSize(
            int totalCount, int pageSize, int expectedPages)
        {
            var response = new PagedResponseDto<UserDto>
            {
                Items = new List<UserDto>(),
                TotalCount = totalCount,
                Page = 1,
                PageSize = pageSize
            };

            response.TotalPages.Should().Be(expectedPages);
        }
    }
}
