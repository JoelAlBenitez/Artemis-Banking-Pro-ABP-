using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
    //Choque con el estado actual del sistema: unicidad violada, recurso ya asociado o un
    //número generado que no pudo ser único. Se traduce a 409 Conflict.
    public sealed class ConflictException : ApplicationLayerException
    {
        public ConflictException(Error error)
            : base(error.Description, new[] { error })
        {
        }

        public ConflictException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
