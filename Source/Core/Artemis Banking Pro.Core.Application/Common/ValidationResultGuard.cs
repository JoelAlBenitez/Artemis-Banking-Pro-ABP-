using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Common
{
    //Los servicios de negocio devuelven ValidationResult porque la aplicación web los consume
    //para pintar mensajes en pantalla. La Web API necesita códigos HTTP, y los errores del
    //dominio no los llevan: cada handler declara qué errores significan 404 y cuáles 409, y
    //el resto se traduce a 400.
    public static class ValidationResultGuard
    {
        //Un fallo de correo no revierte la operación: llega como advertencia y no interrumpe.
        private const string WarningCode = "Advertencia";

        public static void EnsureSuccess(
            ValidationResult result,
            IReadOnlyCollection<Error>? notFound = null,
            IReadOnlyCollection<Error>? conflict = null)
        {
            var failures = result.Errors
                .Where(error => !string.Equals(error.Code, WarningCode, StringComparison.Ordinal))
                .ToList();

            if (failures.Count == 0)
                return;

            if (notFound is not null && failures.Any(failure => notFound.Contains(failure)))
                throw new NotFoundException(failures[0]);

            if (conflict is not null && failures.Any(failure => conflict.Contains(failure)))
                throw new ConflictException(failures[0]);

            throw new BusinessRuleException(failures[0].Description, failures);
        }

        public static T EnsureSuccess<T>(
            ValidationResult<T> result,
            IReadOnlyCollection<Error>? notFound = null,
            IReadOnlyCollection<Error>? conflict = null)
        {
            EnsureSuccess((ValidationResult)result, notFound, conflict);

            if (result.Value is null)
                throw new NotFoundException();

            return result.Value;
        }
    }
}
