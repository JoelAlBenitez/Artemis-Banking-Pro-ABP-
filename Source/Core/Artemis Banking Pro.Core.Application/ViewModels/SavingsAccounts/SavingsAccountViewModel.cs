using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class SavingsAccountViewModel : BaseViewModel<int>
    {
        public required string AccountNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal Balance { get; set; }
        //Etiquetas de presentación: Principal / Secundaria y Activa / Cancelada
        public required string TypeSavingsAccount { get; set; }
        public required string StateSavingsAccount { get; set; }

        //La acción Cancelar solo se muestra en cuentas secundarias activas
        public required bool CanBeCancelled { get; set; }
    }
}
