using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
   
    public abstract class ApplicationLayerException : Exception
    {
        public IReadOnlyCollection<Error> Errors { get; }

        protected ApplicationLayerException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message)
        {
            Errors = errors ?? new List<Error>();
        }

        protected ApplicationLayerException(string message, Exception innerException, IReadOnlyCollection<Error>? errors = null)
            : base(message, innerException)
        {
            Errors = errors ?? new List<Error>();
        }
    }
}
