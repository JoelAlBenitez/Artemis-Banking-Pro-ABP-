using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;

namespace ArtemisBankingPro.Core.Domain.Entities.Commerces
{
    //Vista materializada del pago recibido por un comercio. Evita que la consulta de
    //transacciones tenga que cruzar tarjetas, consumos y cuentas en cada llamada.
    public sealed class CommercePayment : BaseEntitie<int>
    {
        public required int CommerceId { get; set; }

        public required int CreditCardId { get; set; }

        //Único dato de la tarjeta expuesto en la respuesta de la API
        public required string CardLastFourDigits { get; set; }

        public required decimal Amount { get; set; }

        public required int CardConsumptionId { get; set; }

        //CRÉDITO en la cuenta principal del comercio; nulo si el pago fue rechazado
        public int? TransactionId { get; set; }

        public required ConsumptionStatus Status { get; set; }

        public Commerce Commerce { get; set; } = null!;

        public CreditCard CreditCard { get; set; } = null!;
    }
}
