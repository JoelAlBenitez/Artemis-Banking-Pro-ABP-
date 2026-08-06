using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class DepositViewModel
    {
        [Required(ErrorMessage = "The destination account number is required.")]
        public string DestinationAccountNumber { get; set; }

        [Required(ErrorMessage = "The amount to deposit is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "The amount to deposit must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
