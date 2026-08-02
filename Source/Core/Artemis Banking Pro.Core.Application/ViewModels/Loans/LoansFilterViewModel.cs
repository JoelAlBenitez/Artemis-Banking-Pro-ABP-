using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansFilterViewModel
    {
        public string? IdCard { get; set; } 
        public LoanStatusFilter Status { get; set; } = LoanStatusFilter.Activos;

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una página valida.")]
        public int Page { get; set; } = 1;
    }
}
