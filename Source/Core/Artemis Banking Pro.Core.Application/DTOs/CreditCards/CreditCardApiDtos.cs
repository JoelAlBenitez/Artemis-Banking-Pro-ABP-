namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    /// <summary>
    /// Tarjeta de crédito en el listado paginado de la Web API.
    /// </summary>
    /// <remarks>Nunca expone el número completo, el CVC ni su hash.</remarks>
    public class CreditCardListItemDto
    {
        public int Id { get; set; }
        public required string MaskedCardNumber { get; set; }
        public required string LastFourDigits { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CurrentDebt { get; set; }

        //Formato MM/AA
        public required string ExpirationDate { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Consumo registrado en una tarjeta.
    /// </summary>
    public sealed class CardConsumptionApiDto
    {
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }

        //AVANCE cuando el consumo corresponde a un avance de efectivo
        public required string CommerceName { get; set; }

        //APROBADO o RECHAZADO
        public required string Status { get; set; }
    }

    /// <summary>
    /// Detalle de una tarjeta con sus consumos.
    /// </summary>
    public sealed class CreditCardDetailDto : CreditCardListItemDto
    {
        public IReadOnlyCollection<CardConsumptionApiDto> Consumptions { get; set; } = [];
    }
}
