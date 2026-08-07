using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class CashAdvanceViewModel : BaseViewModel<int>
    {
        [Required(ErrorMessage = "Debe seleccionar la tarjeta de crédito origen.")]
        public int CreditCardId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro destino.")]
        public int SavingsAccountId { get; set; }

        [Required(ErrorMessage = "El monto del avance es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto del avance debe ser mayor que cero.")]
        public decimal Amount { get; set; }

        public IReadOnlyCollection<CreditCardDto>? AvailableCards { get; set; } = [];
        public IReadOnlyCollection<SavingsAccountDto>? AvailableAccounts { get; set; } = [];
    }
}
