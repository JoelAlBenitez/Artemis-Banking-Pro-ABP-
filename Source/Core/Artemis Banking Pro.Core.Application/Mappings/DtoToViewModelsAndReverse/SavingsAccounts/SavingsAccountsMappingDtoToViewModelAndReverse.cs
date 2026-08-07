using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.SavingsAccounts
{
    public sealed class SavingsAccountsMappingDtoToViewModelAndReverse : Profile
    {
        public SavingsAccountsMappingDtoToViewModelAndReverse()
        {
            CreateMap<ClientSavingsAccountDto, ClientSavingsAccountViewModel>().ReverseMap();

            //FullNameCustomer solo existe en el ViewModel: el controlador lo completa con el
            //cliente elegido en el paso 1 para mostrarlo en el formulario.
            CreateMap<SavingsAccountAssignmentDto, SavingsAccountAssignmentViewModel>()
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ReverseMap();
            CreateMap<SavingsAccountFilterDto, SavingsAccountFilterViewModel>().ReverseMap();

            //Solo las secundarias activas muestran la acción Cancelar
            CreateMap<SavingsAccountDto, SavingsAccountViewModel>()
                .ForMember(d => d.TypeSavingsAccount,
                    o => o.MapFrom(s => s.TypeSavingsAccount == SavingsAccountType.Principal
                        ? "Principal" : "Secundaria"))
                .ForMember(d => d.StateSavingsAccount,
                    o => o.MapFrom(s => s.StateSavingsAccount == SavingsAccountStatus.Activa
                        ? "Activa" : "Cancelada"))
                .ForMember(d => d.CanBeCancelled,
                    o => o.MapFrom(s => s.TypeSavingsAccount == SavingsAccountType.Secundaria
                        && s.StateSavingsAccount == SavingsAccountStatus.Activa));

            CreateMap<SavingsAccountDto, DetailsSavingsAccountViewModel>()
                .ForMember(d => d.TypeSavingsAccount,
                    o => o.MapFrom(s => s.TypeSavingsAccount == SavingsAccountType.Principal
                        ? "Principal" : "Secundaria"))
                .ForMember(d => d.StateSavingsAccount,
                    o => o.MapFrom(s => s.StateSavingsAccount == SavingsAccountStatus.Activa
                        ? "Activa" : "Cancelada"));

            CreateMap<SavingsAccountDto, CancelSavingsAccountViewModel>()
                .ForMember(d => d.SavingsAccountId, o => o.MapFrom(s => s.Id));

            CreateMap<TransactionDto, TransactionViewModel>()
                .ForMember(d => d.TypeTransaction,
                    o => o.MapFrom(s => s.TypeTransaction == TransactionType.Debito
                        ? "DÉBITO" : "CRÉDITO"))
                .ForMember(d => d.StateTransaction,
                    o => o.MapFrom(s => s.StateTransaction == TransactionStatus.Aprobada
                        ? "APROBADA" : "RECHAZADA"));
        }
    }
}
