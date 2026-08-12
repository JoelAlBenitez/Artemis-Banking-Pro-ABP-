namespace Artemis_Banking_Pro.Core.Application.DTOs.Commerces
{
    //Datos de la tarjeta con la que se procesa un pago. Ninguno de sus valores se registra en
    //logs: ni el número completo, ni el CVC.
    public sealed class ProcessPaymentDto
    {
        public required string CardNumber { get; set; }
        public required string MonthExpirationCard { get; set; }
        public required string YearExpirationCard { get; set; }
        public required string Cvc { get; set; }
        public required decimal TransactionAmount { get; set; }
    }
}
