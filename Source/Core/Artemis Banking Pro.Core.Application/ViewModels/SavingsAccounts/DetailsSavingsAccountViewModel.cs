using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class DetailsSavingsAccountViewModel : BaseViewModel<int>
    {
        public required string AccountNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal Balance { get; set; }
        public required string TypeSavingsAccount { get; set; }
        public required string StateSavingsAccount { get; set; }
    }
}
