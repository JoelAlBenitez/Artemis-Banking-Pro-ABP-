namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    //Modelo de la pantalla del listado: filtro aplicado + préstamos de la página + paginación.
    public sealed class LoansListViewModel
    {
        public required LoansFilterViewModel Filter { get; set; }
        public required IReadOnlyCollection<LoansViewModel> Loans { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
