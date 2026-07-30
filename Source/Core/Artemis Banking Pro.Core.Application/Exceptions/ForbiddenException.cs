using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
    
    public sealed class ForbiddenException : ApplicationLayerException
    {
        private const string DefaultMessage = "No tiene permisos para realizar esta acción.";

        public ForbiddenException()
            : base(DefaultMessage)
        {
        }

        public ForbiddenException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
