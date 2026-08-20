namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    //Modelo de la pantalla del listado: filtro aplicado + cuentas de la página + paginación.
    public sealed class SavingsAccountsListViewModel
    {
        public required SavingsAccountFilterViewModel Filter { get; set; }
        public required IReadOnlyCollection<SavingsAccountViewModel> SavingsAccounts { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
