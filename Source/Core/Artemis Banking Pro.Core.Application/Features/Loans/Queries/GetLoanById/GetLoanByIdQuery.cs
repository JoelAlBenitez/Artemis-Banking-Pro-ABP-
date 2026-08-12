using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using MediatR;

namespace Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetLoanById
{
    public class GetLoanByIdQuery : IRequest<LoanDetailApiDto>
    {
        public required int Id { get; set; }
    }

    public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, LoanDetailApiDto>
    {
        private const string UpToDate = "Al día";
        private const string InArrears = "En mora";

        private static readonly Error[] NotFoundErrors = [LoansError.NonExistsLoan];

        private readonly ILoansServices _loansServices;
        private readonly IMapper _mapper;

        public GetLoanByIdQueryHandler(ILoansServices loansServices, IMapper mapper)
        {
            _loansServices = loansServices;
            _mapper = mapper;
        }

        public async Task<LoanDetailApiDto> Handle(
            GetLoanByIdQuery query, CancellationToken cancellationToken)
        {
            var result = await _loansServices.GetDetailLoanAsync(query.Id);
            var detail = ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);

            var apiDetail = _mapper.Map<LoanDetailApiDto>(detail);

            //Los tres se derivan de la tabla de amortización, que ya viene cargada
            apiDetail.MonthlyInstallment = detail.loansInstallmentDtos.Count == 0
                ? 0m
                : detail.loansInstallmentDtos[0].InstallmentValue;

            apiDetail.PendingAmount = detail.loansInstallmentDtos.Sum(installment => installment.OutstandingBalance);

            apiDetail.ClientPaymentStatus = detail.loansInstallmentDtos.Any(installment => installment.IsOverdue)
                ? InArrears
                : UpToDate;

            return apiDetail;
        }
    }
}
