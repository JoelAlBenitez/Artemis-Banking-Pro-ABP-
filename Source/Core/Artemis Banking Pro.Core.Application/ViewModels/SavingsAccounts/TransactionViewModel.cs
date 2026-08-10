namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class TransactionViewModel
    {
        public required DateTimeOffset TransactionDate { get; set; }
        public required decimal Amount { get; set; }
        //Etiquetas de presentación: DÉBITO / CRÉDITO y APROBADA / RECHAZADA
        public required string TypeTransaction { get; set; }
        public required string Beneficiary { get; set; }
        public required string Origin { get; set; }
        public required string StateTransaction { get; set; }
    }
}
