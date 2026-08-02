namespace Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts
{
    public interface IAccountNumberGenerator
    {
        //Emite un número de 9 dígitos libre verificando simultáneamente cuentas de ahorro y
        //préstamos. Devuelve null cuando se agotan los intentos acotados sin obtener uno libre.
        Task<string?> GenerateUniqueAccountNumberAsync();
    }
}
