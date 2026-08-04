using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Beneficiaries
{
    public class SaveBeneficiaryViewModel : BaseViewModel<int>
    {
        [SetsRequiredMembers]
        public SaveBeneficiaryViewModel()
        {
            AccountNumber = null!;
        }

        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "El número de cuenta solo debe contener números.")]
        public required string AccountNumber { get; set; }
    }
}
