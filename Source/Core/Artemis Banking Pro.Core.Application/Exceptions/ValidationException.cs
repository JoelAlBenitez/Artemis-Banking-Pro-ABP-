using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
   
    public sealed class ValidationException : ApplicationLayerException
    {
        private const string DefaultMessage = "La solicitud contiene datos inválidos.";

        public ValidationException()
            : base(DefaultMessage)
        {
        }

        public ValidationException(params Error[] errors)
            : base(DefaultMessage, errors)
        {
        }

        public ValidationException(IReadOnlyCollection<Error> errors)
            : base(DefaultMessage, errors)
        {
        }

        public ValidationException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
