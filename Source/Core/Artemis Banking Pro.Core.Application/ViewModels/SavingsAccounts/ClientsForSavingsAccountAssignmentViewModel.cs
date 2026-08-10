namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class ClientsForSavingsAccountAssignmentViewModel
    {
        public required IReadOnlyCollection<ClientSavingsAccountViewModel> Clients { get; set; }
        public string? IdCard { get; set; }
    }
}
