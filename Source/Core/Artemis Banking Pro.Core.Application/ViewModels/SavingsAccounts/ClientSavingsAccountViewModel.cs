using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    //Fila del listado de clientes activos del paso 1: selección por radio button, uno a la vez.
    public sealed class ClientSavingsAccountViewModel : BaseViewModel<string>
    {
        public required string IdCard { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required decimal TotalDebtAmount { get; set; }
    }
}
