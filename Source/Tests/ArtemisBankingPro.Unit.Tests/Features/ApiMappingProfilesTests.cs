using FluentAssertions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features
{
    public sealed class ApiMappingProfilesTests
    {
        //Un miembro sin mapear deja un campo vacío en la respuesta de la API sin fallar en
        //tiempo de compilación: la validación de la configuración es la única red que lo atrapa.
        [Fact]
        public void Configuration_ShouldBeValid()
        {
            var act = () => ApiMapperFactory.BuildConfiguration().AssertConfigurationIsValid();

            act.Should().NotThrow();
        }
    }
}
