namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    //Modelo de la pantalla del listado: filtro aplicado + tarjetas de la página + paginación.
    public sealed class CreditCardsListViewModel
    {
        public required CreditCardFilterViewModel Filter { get; set; }
        public required IReadOnlyCollection<CreditCardViewModel> CreditCards { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
