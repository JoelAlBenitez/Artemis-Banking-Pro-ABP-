using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class CreditCardFilterViewModel
    {
        [StringLength(11, ErrorMessage = "Debe ingresar una cédula no mayor a 11 digitos sin guiones.")]
        public string? IdCard { get; set; }

        [EnumDataType(typeof(CreditCardStatusFilter), ErrorMessage = "Debe seleccionar un estado valido.")]
        public CreditCardStatusFilter Status { get; set; } = CreditCardStatusFilter.Activas;

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una página valida.")]
        public int Page { get; set; } = 1;
    }
}
