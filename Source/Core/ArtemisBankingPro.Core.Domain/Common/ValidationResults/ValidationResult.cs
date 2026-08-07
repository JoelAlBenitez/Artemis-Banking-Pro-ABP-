using ArtemisBankingPro.Core.Domain.Common.Errors;
namespace ArtemisBankingPro.Core.Domain.Common.ValidationResult
{
    public class ValidationResult
    {
        public IReadOnlyCollection<Error> Errors { get; }
        public bool IsValid => Errors == null || Errors.Count == 0;

        public ValidationResult(IReadOnlyCollection<Error> Errors)
        {
            this.Errors = Errors ?? new List<Error>();
        }

        public static ValidationResult Success()
            => new ValidationResult(new List<Error>());

        public static ValidationResult Failure(params Error[] Errors)
            => new ValidationResult(Errors);

        public static ValidationResult Failure(List<Error> Errors)
            => new ValidationResult(Errors);
    }

    public class ValidationResult<T> : ValidationResult
    {
        public T? Value { get; }

        public ValidationResult(T? value, IReadOnlyCollection<Error> Errors) : base(Errors)
        {
            Value = value;
        }

        public static ValidationResult<T> Success(T value)
            => new ValidationResult<T>(value, new List<Error>());

        public static new ValidationResult<T> Failure(params Error[] Errors)
            => new ValidationResult<T>(default, Errors);

        public static new ValidationResult<T> Failure(List<Error> Errors)
            => new ValidationResult<T>(default, Errors);
    }
}