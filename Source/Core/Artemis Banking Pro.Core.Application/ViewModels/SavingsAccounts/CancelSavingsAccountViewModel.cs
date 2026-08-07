using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class CancelSavingsAccountViewModel
    {
        [Required(ErrorMessage = "Debe indicar una cuenta de ahorro valida.")]
        public required int SavingsAccountId { get; set; }

        public required string AccountNumber { get; set; }

        public string ConfirmationMessage => $"¿Está seguro que desea cancelar la cuenta {AccountNumber}?";
    }
}
