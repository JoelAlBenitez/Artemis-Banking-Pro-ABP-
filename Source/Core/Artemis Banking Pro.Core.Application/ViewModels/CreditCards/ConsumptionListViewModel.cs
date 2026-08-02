namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    //Modelo de la pantalla de detalles: tarjeta consultada + consumos de la página + paginación.
    public sealed class ConsumptionListViewModel
    {
        public required DetailsCreditCardViewModel CreditCard { get; set; }
        public required IReadOnlyCollection<CardConsumptionViewModel> Consumptions { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
