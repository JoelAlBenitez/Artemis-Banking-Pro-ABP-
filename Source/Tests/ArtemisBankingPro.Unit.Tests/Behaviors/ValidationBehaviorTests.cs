using Artemis_Banking_Pro.Core.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;
using ValidationException = Artemis_Banking_Pro.Core.Application.Exceptions.ValidationException;

namespace ArtemisBankingPro.Unit.Tests.Behaviors
{
    //El behavior es la única puerta de las validaciones estructurales de la API. Si dejara
    //pasar una petición inválida, la regla terminaría comprobándose dentro del handler o,
    //peor, contra la base de datos.
    public sealed class ValidationBehaviorTests
    {
        private const string HandlerResponse = "ejecutado";

        public sealed class SampleRequest : IRequest<string>
        {
            public string Name { get; set; } = string.Empty;
            public int Amount { get; set; }
        }

        private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
        {
            public SampleRequestValidator()
            {
                RuleFor(request => request.Name).NotEmpty().WithMessage("El nombre es obligatorio.");
                RuleFor(request => request.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor que cero.");
            }
        }

        [Fact]
        public async Task Handle_WithoutValidators_ShouldExecuteTheHandler()
        {
            var behavior = new ValidationBehavior<SampleRequest, string>([]);

            var result = await behavior.Handle(new SampleRequest(), Next, CancellationToken.None);

            result.Should().Be(HandlerResponse);
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldExecuteTheHandler()
        {
            var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);
            var request = new SampleRequest { Name = "Tienda Demo", Amount = 500 };

            var result = await behavior.Handle(request, Next, CancellationToken.None);

            result.Should().Be(HandlerResponse);
        }

        [Fact]
        public async Task Handle_WithInvalidRequest_ShouldNotExecuteTheHandler()
        {
            var handlerWasCalled = false;
            var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);

            var act = async () => await behavior.Handle(
                new SampleRequest(),
                _ => { handlerWasCalled = true; return Task.FromResult(HandlerResponse); },
                CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            handlerWasCalled.Should().BeFalse();
        }

        //Los errores se acumulan: el consumidor de la API debe recibir todo lo que está mal en
        //una sola respuesta, no descubrirlo campo por campo.
        [Fact]
        public async Task Handle_WithSeveralBrokenRules_ShouldReportThemAllTogether()
        {
            var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);

            var act = async () => await behavior.Handle(
                new SampleRequest { Name = string.Empty, Amount = 0 }, Next, CancellationToken.None);

            var exception = await act.Should().ThrowAsync<ValidationException>();

            exception.Which.Errors.Select(error => error.Description)
                .Should().BeEquivalentTo(
                    "El nombre es obligatorio.",
                    "El monto debe ser mayor que cero.");
        }

        //FluentValidation trae su propia ValidationException: la que sale del behavior debe ser
        //la de la capa de aplicación, que es la que el Global Exception Handler traduce a 400.
        [Fact]
        public async Task Handle_WithInvalidRequest_ShouldThrowTheApplicationValidationException()
        {
            var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);

            var act = async () => await behavior.Handle(new SampleRequest(), Next, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            await act.Should().NotThrowAsync<FluentValidation.ValidationException>();
        }

        private static Task<string> Next(CancellationToken cancellationToken)
            => Task.FromResult(HandlerResponse);
    }
}
