using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ThirdPartyTransferViewModel
    {
        [Required(ErrorMessage = "The origin account number is required.")]
        public string OriginAccountNumber { get; set; }

        [Required(ErrorMessage = "The destination account number is required.")]
        public string DestinationAccountNumber { get; set; }

        [Required(ErrorMessage = "The amount to transfer is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "The amount to transfer must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
