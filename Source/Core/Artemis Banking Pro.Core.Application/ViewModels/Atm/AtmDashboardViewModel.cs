namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class AtmDashboardViewModel
    {
        public int TransactionsMadeToday { get; set; }
        public int PaymentsMadeToday { get; set; }
        public int DepositsMadeToday { get; set; }
        public int WithdrawalsMadeToday { get; set; }
    }
}
