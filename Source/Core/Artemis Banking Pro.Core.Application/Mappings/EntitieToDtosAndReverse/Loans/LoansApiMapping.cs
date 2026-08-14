using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans
{
    public sealed class LoansApiMapping : Profile
    {
        private const string UpToDate = "Al día";
        private const string InArrears = "En mora";

        public LoansApiMapping()
        {
            CreateMap<LoansDto, LoanListItemDto>()
                .ForMember(dto => dto.ClientId, opt => opt.MapFrom(source => source.CustomerId))
                .ForMember(dto => dto.ClientFullName, opt => opt.MapFrom(source => source.FullNameCustomer))
                .ForMember(dto => dto.CapitalAmount, opt => opt.MapFrom(source => source.AprovechedCapital))
                .ForMember(dto => dto.TotalInstallments, opt => opt.MapFrom(source => source.QuantityInstallment))
                .ForMember(dto => dto.PaidInstallments, opt => opt.MapFrom(source => source.InstallmentPay))
                .ForMember(dto => dto.PendingAmount, opt => opt.MapFrom(source => source.PendientAmount))
                .ForMember(dto => dto.TermInMonths, opt => opt.MapFrom(source => source.Term))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(source => source.StateLoans.ToString()))
                .ForMember(dto => dto.ClientPaymentStatus,
                    opt => opt.MapFrom(source => source.CustomerInArrears ? InArrears : UpToDate));

            CreateMap<LoansInstallmentDto, LoanInstallmentApiDto>()
                .ForMember(dto => dto.InstallmentNumber, opt => opt.MapFrom(source => source.NumberLoanInstallment))
                .ForMember(dto => dto.InstallmentAmount, opt => opt.MapFrom(source => source.InstallmentValue))
                .ForMember(dto => dto.PendingInstallmentAmount, opt => opt.MapFrom(source => source.OutstandingBalance))
                .ForMember(dto => dto.PaymentStatus, opt => opt.MapFrom(source => source.StateInstallment.ToString()))
                .ForMember(dto => dto.IsLate, opt => opt.MapFrom(source => source.IsOverdue));

            CreateMap<DetailLoansDto, LoanDetailApiDto>()
                .ForMember(dto => dto.LoanNumber, opt => opt.MapFrom(source => source.NumberLoand))
                .ForMember(dto => dto.ClientId, opt => opt.MapFrom(source => source.CustomerId))
                .ForMember(dto => dto.ClientFullName, opt => opt.MapFrom(source => source.FullNameCustomer))
                .ForMember(dto => dto.CapitalAmount, opt => opt.MapFrom(source => source.ApprovedAmount))
                .ForMember(dto => dto.TermInMonths, opt => opt.MapFrom(source => source.Term))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(source => source.StateLoans.ToString()))
                .ForMember(dto => dto.Amortization, opt => opt.MapFrom(source => source.loansInstallmentDtos))
                //Se derivan de la tabla de amortización: los calcula el handler
                .ForMember(dto => dto.MonthlyInstallment, opt => opt.Ignore())
                .ForMember(dto => dto.PendingAmount, opt => opt.Ignore())
                .ForMember(dto => dto.ClientPaymentStatus, opt => opt.Ignore());
        }
    }
}
