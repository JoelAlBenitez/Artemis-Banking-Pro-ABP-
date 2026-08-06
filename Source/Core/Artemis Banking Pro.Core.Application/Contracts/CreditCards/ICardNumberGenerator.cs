namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    public interface ICardNumberGenerator
    {
        //Devuelve null cuando se agotan los intentos acotados sin obtener un número libre.
        Task<string?> GenerateUniqueCardNumberAsync();
    }
}
