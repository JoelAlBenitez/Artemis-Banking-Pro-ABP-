namespace Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard
{
    //Los 11 indicadores del Home del administrador (documento funcional, págs. 17-20).
    public sealed class AdminDashboardDto
    {
        public required int TotalHistoricalTransactions { get; set; }
        public required int DayTransactions { get; set; }
        public required int TotalHistoricalPay {  get; set; }
        public required int DayPay {  get; set; }
        public required int CustomerActive { get; set; }
        public required int CustomerInactive { get; set; }
        public required int TotalFinancialProducts { get; set; }
        public required int OutstandingLoans { get; set; }
        public required int CreditCardActive { get; set; }
        public required int SavingAccountActive { get; set; }

        //Monto, no conteo: se muestra como RD$0.00 y necesita los dos decimales.
        public required decimal AverageDebtAmountPerCustomer { get; set; }
    }
}
