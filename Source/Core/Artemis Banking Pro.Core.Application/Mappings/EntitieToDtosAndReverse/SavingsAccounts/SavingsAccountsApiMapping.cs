using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts
{
    //Los enums viajan como texto en la API: el documento los muestra escritos («Principal»,
    //«Activa», «CRÉDITO»). La conversión vive aquí y no en cada handler.
    public sealed class SavingsAccountsApiMapping : Profile
    {
        private const string Debit = "DÉBITO";
        private const string Credit = "CRÉDITO";
        private const string Approved = "APROBADA";
        private const string Rejected = "RECHAZADA";

        public SavingsAccountsApiMapping()
        {
            CreateMap<SavingsAccountDto, SavingsAccountListItemDto>()
                .ForMember(dto => dto.ClientId, opt => opt.MapFrom(source => source.CustomerId))
                .ForMember(dto => dto.ClientFullName, opt => opt.MapFrom(source => source.FullNameCustomer))
                .ForMember(dto => dto.Identification, opt => opt.MapFrom(source => source.IdCard))
                .ForMember(dto => dto.Type, opt => opt.MapFrom(source => source.TypeSavingsAccount.ToString()))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(source => source.StateSavingsAccount.ToString()));

            CreateMap<SavingsAccountDto, SavingsAccountCreatedDto>()
                .ForMember(dto => dto.ClientId, opt => opt.MapFrom(source => source.CustomerId))
                .ForMember(dto => dto.ClientFullName, opt => opt.MapFrom(source => source.FullNameCustomer))
                .ForMember(dto => dto.Type, opt => opt.MapFrom(source => source.TypeSavingsAccount.ToString()))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(source => source.StateSavingsAccount.ToString()));

            CreateMap<TransactionDto, TransactionApiDto>()
                .ForMember(dto => dto.Date, opt => opt.MapFrom(source => source.TransactionDate))
                .ForMember(dto => dto.TransactionType,
                    opt => opt.MapFrom(source => source.TypeTransaction == TransactionType.Debito ? Debit : Credit))
                .ForMember(dto => dto.Status,
                    opt => opt.MapFrom(source => source.StateTransaction == TransactionStatus.Aprobada ? Approved : Rejected));
        }
    }
}
