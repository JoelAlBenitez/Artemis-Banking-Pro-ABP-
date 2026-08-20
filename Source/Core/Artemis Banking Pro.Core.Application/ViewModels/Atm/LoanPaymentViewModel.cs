using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class LoanPaymentViewModel
    {
        [Required(ErrorMessage = "The origin account number is required.")]
        public string OriginAccountNumber { get; set; }

        [Required(ErrorMessage = "The loan number is required.")]
        public string LoanNumber { get; set; }

        [Required(ErrorMessage = "The amount to pay is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "The amount to pay must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
