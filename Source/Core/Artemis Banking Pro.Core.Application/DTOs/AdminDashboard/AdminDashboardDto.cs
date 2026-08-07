using System.Runtime.ConstrainedExecution;

namespace Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard
{
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
        public required int SavingAccountActive { get; set; }
        public required int AverageDebtAmountPerCustomer { get; set; }

    }
}
