namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    //Resultado de la cancelación de una cuenta secundaria usado por la notificación al cliente.
    public sealed class SavingsAccountCancelledDto
    {
        public required string CustomerId { get; set; }
        public required string AccountNumber { get; set; }

        //RD$0.00 cuando la cuenta se cancela sin transferencia de fondos
        public required decimal TransferredAmount { get; set; }
        public required string PrimaryAccountNumber { get; set; }
        public required DateTimeOffset CancelledAt { get; set; }
    }
}
