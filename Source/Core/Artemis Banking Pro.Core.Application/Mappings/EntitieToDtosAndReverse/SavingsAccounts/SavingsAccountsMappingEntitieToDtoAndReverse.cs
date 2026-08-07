using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts
{
    public sealed class SavingsAccountsMappingEntitieToDtoAndReverse : Profile
    {
        public SavingsAccountsMappingEntitieToDtoAndReverse()
        {
            //El nombre y la cédula del cliente provienen de Identity y los completa el servicio.
            CreateMap<SavingsAccount, SavingsAccountDto>()
                .ForMember(d => d.TypeSavingsAccount, o => o.MapFrom(s => s.AccountType))
                .ForMember(d => d.StateSavingsAccount, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ForMember(d => d.IdCard, o => o.Ignore());

            //Campos de solo sistema: número de cuenta, tipo, estado y auditoría.
            //Desde el módulo administrador toda cuenta se crea Secundaria y Activa.
            CreateMap<SavingsAccountAssignmentDto, SavingsAccount>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AccountNumber, o => o.Ignore())
                .ForMember(d => d.Balance, o => o.MapFrom(s => s.InitialBalance))
                .ForMember(d => d.AccountType, o => o.MapFrom(_ => SavingsAccountType.Secundaria))
                .ForMember(d => d.Status, o => o.MapFrom(_ => SavingsAccountStatus.Activa))
                .ForMember(d => d.StatusChangedAt, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreateByUserId, o => o.Ignore())
                .ForMember(d => d.LastModifiedByIdUser, o => o.Ignore())
                .ForMember(d => d.ModifiedAt, o => o.Ignore());

            //El mapeo de Transaction a TransactionDto se agrega cuando el módulo Cliente
            //exponga la entidad; el contrato de lectura ya existe en este módulo.
        }
    }
}
