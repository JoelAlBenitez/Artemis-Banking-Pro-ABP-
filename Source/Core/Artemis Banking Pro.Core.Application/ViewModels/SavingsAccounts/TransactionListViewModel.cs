namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    //Modelo de la pantalla de detalles: cuenta consultada + transacciones de la página + paginación.
    public sealed class TransactionListViewModel
    {
        public required DetailsSavingsAccountViewModel SavingsAccount { get; set; }
        public required IReadOnlyCollection<TransactionViewModel> Transactions { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
