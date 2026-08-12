using ArtemisBankingPro.Core.Domain.Common.Errors;
using FluentValidation;
using MediatR;
//FluentValidation trae su propia ValidationException: el alias evita que se cuele en lugar
//de la excepción de la capa de aplicación que el Global Exception Handler sabe traducir.
using ValidationException = Artemis_Banking_Pro.Core.Application.Exceptions.ValidationException;

namespace Artemis_Banking_Pro.Core.Application.Behaviors
{
    //Validación estructural de todo Command y Query antes de llegar a su handler. Las reglas
    //que necesitan consultar la base de datos no viven aquí: pertenecen al handler.
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const string ValidationErrorCode = "VALIDACION";

        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next(cancellationToken);

            var context = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            var errors = results
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .Select(failure => new Error(ValidationErrorCode, failure.ErrorMessage))
                .ToList();

            if (errors.Count > 0)
                throw new ValidationException(errors);

            return await next(cancellationToken);
        }
    }
}
