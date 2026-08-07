using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    public sealed class SavingsAccountDto : BaseDto<int>
    {
        public required string AccountNumber { get; set; }
        public required string CustomerId { get; set; }

        //Provienen del project Identity, no de la entidad: el perfil de AutoMapper los ignora
        //y el servicio los completa al armar el listado.
        public required string FullNameCustomer { get; set; }
        public required string IdCard { get; set; }

        public required decimal Balance { get; set; }
        public required SavingsAccountType TypeSavingsAccount { get; set; }
        public required SavingsAccountStatus StateSavingsAccount { get; set; }
    }
}
