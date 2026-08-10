using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class SavingsAccountFilterViewModel
    {
        [StringLength(11, ErrorMessage = "Debe ingresar una cédula no mayor a 11 digitos sin guiones.")]
        public string? IdCard { get; set; }

        [EnumDataType(typeof(SavingsAccountStatusFilter), ErrorMessage = "Debe seleccionar un estado valido.")]
        public SavingsAccountStatusFilter Status { get; set; } = SavingsAccountStatusFilter.Activas;

        [EnumDataType(typeof(SavingsAccountTypeFilter), ErrorMessage = "Debe seleccionar un tipo de cuenta valido.")]
        public SavingsAccountTypeFilter Type { get; set; } = SavingsAccountTypeFilter.Todas;

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una página valida.")]
        public int Page { get; set; } = 1;
    }
}
