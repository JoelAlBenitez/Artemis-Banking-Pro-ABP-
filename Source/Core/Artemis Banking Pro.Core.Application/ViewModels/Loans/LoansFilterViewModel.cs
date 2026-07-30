using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansFilterViewModel
    {
     

        [EnumDataType(typeof(LoanStatusFilter), ErrorMessage = "Debe seleccionar un estado valido.")]
        public LoanStatusFilter Status { get; set; } = LoanStatusFilter.Todos;

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una página valida.")]
        public int Page { get; set; } = 1;
    }
}
