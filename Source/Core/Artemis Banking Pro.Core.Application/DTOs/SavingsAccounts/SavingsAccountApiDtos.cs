using ArtemisBankingPro.Core.Application.DTOs.Common;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    /// <summary>
    /// Cuenta de ahorro en el listado paginado de la Web API.
    /// </summary>
    public sealed class SavingsAccountListItemDto
    {
        public int Id { get; set; }
        public required string AccountNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public required string Identification { get; set; }
        public decimal Balance { get; set; }
        public required string Type { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Cuenta secundaria recién asignada.
    /// </summary>
    public sealed class SavingsAccountCreatedDto
    {
        public int Id { get; set; }
        public required string AccountNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public decimal Balance { get; set; }
        public required string Type { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Transacción del historial de una cuenta.
    /// </summary>
    public sealed class TransactionApiDto
    {
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }

        //DÉBITO o CRÉDITO, tal como los nombra el documento funcional
        public required string TransactionType { get; set; }
        public required string Origin { get; set; }
        public string? Beneficiary { get; set; }

        //APROBADA o RECHAZADA
        public required string Status { get; set; }
    }

    /// <summary>
    /// Cuenta consultada con su historial de transacciones paginado.
    /// </summary>
    /// <remarks>
    /// Es el único listado de la API cuya paginación va anidada, tal como lo muestra el
    /// documento funcional.
    /// </remarks>
    public sealed class AccountTransactionsDto
    {
        public required string AccountNumber { get; set; }
        public required string ClientFullName { get; set; }
        public decimal Balance { get; set; }
        public required string Type { get; set; }
        public required string Status { get; set; }
        public required PagedApiResponse<TransactionApiDto> Transactions { get; set; }
    }
}
