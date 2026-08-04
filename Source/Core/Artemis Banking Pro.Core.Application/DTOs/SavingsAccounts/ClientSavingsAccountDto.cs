using Artemis_Banking_Pro.Core.Application.DTOs.Base;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    //Fila del listado de clientes activos del paso 1 de la asignación.
    //Los datos provienen del project Identity y la deuda del cálculo de préstamos y tarjetas activas.
    public sealed class ClientSavingsAccountDto : BaseDto<string>
    {
        public required string IdCard { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required decimal TotalDebtAmount { get; set; }
    }
}
