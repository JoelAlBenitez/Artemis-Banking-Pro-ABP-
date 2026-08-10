using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Dashboard
{
    public class ClientDashboardViewModel
    {
        public IReadOnlyCollection<SavingsAccountDto> SavingsAccounts { get; set; } = new List<SavingsAccountDto>();
        public IReadOnlyCollection<LoansDto> Loans { get; set; } = new List<LoansDto>();
        public IReadOnlyCollection<CreditCardDto> CreditCards { get; set; } = new List<CreditCardDto>();
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
    }
}
