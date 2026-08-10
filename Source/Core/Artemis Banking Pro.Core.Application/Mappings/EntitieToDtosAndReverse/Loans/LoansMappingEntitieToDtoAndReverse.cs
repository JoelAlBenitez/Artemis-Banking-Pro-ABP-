using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans
{
    public sealed class LoansMappingEntitieToDtoAndReverse : Profile
    {
        public LoansMappingEntitieToDtoAndReverse()
        {
            //El nombre del cliente proviene de Identity y lo completa el servicio
            
            
            CreateMap<Loan, LoansDto>()
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ForMember(d => d.AprovechedCapital, o => o.MapFrom(s => s.ApprovedCapital))
                .ForMember(d => d.QuantityInstallment, o => o.MapFrom(s => s.loanInstallments.Count))
                .ForMember(d => d.InstallmentPay,
                    o => o.MapFrom(s => s.loanInstallments.Count(i => i.paymentStatus == PaymentStatus.Pagada)))
                .ForMember(d => d.PendientAmount, o => o.MapFrom(s => s.PendingAmount))
                .ForMember(d => d.Term, o => o.MapFrom(s => (int)s.termMonths))
                .ForMember(d => d.StateLoans, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.CustomerInArrears,
                    o => o.MapFrom(s => s.loanInstallments.Any(i => i.IsOverdue)));

            CreateMap<Loan, DetailLoansDto>()
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ForMember(d => d.NumberLoand, o => o.MapFrom(s => s.LoanNumber))
                .ForMember(d => d.ApprovedAmount, o => o.MapFrom(s => s.ApprovedCapital))
                .ForMember(d => d.Term, o => o.MapFrom(s => (int)s.termMonths))
                .ForMember(d => d.StateLoans, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.loansInstallmentDtos,
                    o => o.MapFrom(s => s.loanInstallments.OrderBy(i => i.InstallmentNumber)));

            CreateMap<LoanInstallment, LoansInstallmentDto>()
                .ForMember(d => d.NumberLoanInstallment, o => o.MapFrom(s => s.InstallmentNumber))
                .ForMember(d => d.OutstandingBalance, o => o.MapFrom(s => s.PendingBalance))
                .ForMember(d => d.StateInstallment, o => o.MapFrom(s => s.paymentStatus));


            //
            CreateMap<Loan, EditAnnualInterestRateDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.AnnualInterestRate, o => o.MapFrom(s => s.AnnualInterestRate));


            //Campos de solo sistema: numero, cuota, totales, estado y auditoria
            CreateMap<LoansAssignmentDto, Loan>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.LoanNumber, o => o.Ignore())
                .ForMember(d => d.ApprovedCapital, o => o.MapFrom(s => s.AmmountLoans))
                .ForMember(d => d.termMonths, o => o.MapFrom(s => s.TermLoans))
                .ForMember(d => d.MonthlyInstallment, o => o.Ignore())
                .ForMember(d => d.TotalPayable, o => o.Ignore())
                .ForMember(d => d.PendingAmount, o => o.Ignore())
                .ForMember(d => d.Status, o => o.MapFrom(_ => LoanStatus.Activo))
                .ForMember(d => d.CompletedAt, o => o.Ignore())
                .ForMember(d => d.loanInstallments, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreateByUserId, o => o.Ignore())
                .ForMember(d => d.LastModifiedByIdUser, o => o.Ignore())
                .ForMember(d => d.ModifiedAt, o => o.Ignore());
        }
    }
}
