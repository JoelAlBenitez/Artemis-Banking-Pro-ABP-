namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class CardConsumptionViewModel
    {
        public required DateTimeOffset ConsumptionDate { get; set; }
        public required decimal Amount { get; set; }
        public required string CommerceName { get; set; }
        //Etiqueta de presentación: APROBADO / RECHAZADO
        public required string StateConsumption { get; set; }
    }
}
