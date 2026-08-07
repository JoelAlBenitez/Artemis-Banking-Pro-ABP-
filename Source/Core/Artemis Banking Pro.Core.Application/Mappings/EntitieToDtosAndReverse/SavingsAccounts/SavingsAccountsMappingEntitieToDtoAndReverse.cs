using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts
{
    public sealed class SavingsAccountsMappingEntitieToDtoAndReverse : Profile
    {
        public SavingsAccountsMappingEntitieToDtoAndReverse()
        {
            //El nombre y la cédula del cliente provienen de Identity y los completa el servicio.
            //El ReverseMap lo consume el Dashboard del módulo Cliente, que materializa la
            //entidad a partir del DTO: ambos sentidos viven en este único perfil.
            CreateMap<SavingsAccount, SavingsAccountDto>()
                .ForMember(d => d.TypeSavingsAccount, o => o.MapFrom(s => s.AccountType))
                .ForMember(d => d.StateSavingsAccount, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ForMember(d => d.IdCard, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.AccountType, o => o.MapFrom(s => s.TypeSavingsAccount))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.StateSavingsAccount))
                .ForMember(d => d.StatusChangedAt, o => o.Ignore())
                .ForMember(d => d.Transactions, o => o.Ignore());

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
                .ForMember(d => d.ModifiedAt, o => o.Ignore())
                //El historial lo escribe el servicio, nunca el mapeo de la asignación.
                .ForMember(d => d.Transactions, o => o.Ignore());

            //Proyección del historial mostrado en el detalle de la cuenta. Beneficiary es
            //opcional en la entidad y obligatorio en el DTO: las transacciones sin destino
            //(retiros, depósitos) se proyectan con cadena vacía.
            CreateMap<Transaction, TransactionDto>()
                .ForMember(d => d.TransactionDate, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.TypeTransaction, o => o.MapFrom(s => s.TransactionType))
                .ForMember(d => d.StateTransaction, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.Beneficiary, o => o.MapFrom(s => s.Beneficiary ?? string.Empty));
        }
    }
}
