<<<<<<< HEAD
=======
using System.Collections.Generic;
>>>>>>> origin/development
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class AccountTransferViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de origen.")]
        public int SourceAccountId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de destino.")]
        public int DestinationAccountId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }
<<<<<<< HEAD
=======

        public IReadOnlyCollection<SavingsAccountSelectViewModel>? AvailableAccounts { get; set; } = [];
>>>>>>> origin/development
    }
}
