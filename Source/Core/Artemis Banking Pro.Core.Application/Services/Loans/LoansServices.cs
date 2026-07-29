using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
    public sealed class LoansServices : ILoansServices
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly IAmortizationCalculator _amortizationCalculator;
        private readonly IEmailServices _emailServices;
        private readonly IMapper _mapper;

        public LoansServices(
            ILoansRepository loansRepository,
            ILoanInstallmentRepository loanInstallmentRepository,
            IAmortizationCalculator amortizationCalculator,
            IEmailServices emailServices,
            IMapper mapper)
        {
            _loansRepository = loansRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            _amortizationCalculator = amortizationCalculator;
            _emailServices = emailServices;
            _mapper = mapper;
        }

        public async Task<ValidationResult<PagedResult<LoansDto>>> GetPagedLoansAsync(
            LoansFilterDto filter, string? customerId)
        {
            try
            {
                var result = await _loansRepository.GetPagedLoansAsync(
                    filter.Page,
                    DomainConstants.DefaultPageSize,
                    ToLoanStatus(filter.Status),
                    customerId);

                if (!string.IsNullOrWhiteSpace(customerId) && result.TotalRecords == 0)
                {
                    return ValidationResult<PagedResult<LoansDto>>.Failure(LoandError.NonExistsLoans);
                }

                var items = _mapper.Map<IReadOnlyCollection<LoansDto>>(result.Items);
                var paged = new PagedResult<LoansDto>(
                    items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<LoansDto>>.Success(paged);
            }
            catch (Exception)
            {
                return ValidationResult<PagedResult<LoansDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<DetailLoansDto>> GetDetailLoanAsync(int loanId)
        {
            try
            {
                var loan = await _loansRepository.GetLoanWithInstallmentsAsync(loanId);
                if (loan is null)
                {
                    return ValidationResult<DetailLoansDto>.Failure(LoandError.NonExistsLoan);
                }

                return ValidationResult<DetailLoansDto>.Success(_mapper.Map<DetailLoansDto>(loan));
            }
            catch (Exception)
            {
                return ValidationResult<DetailLoansDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<EditAnnualInterestRateDto>> GetLoanForEditRateAsync(int loanId)
        {
            try
            {
                var loan = await _loansRepository.GetByIdAsync(loanId);
                if (loan is null)
                {
                    return ValidationResult<EditAnnualInterestRateDto>.Failure(LoandError.NonExistsLoan);
                }

                if (loan.Status != LoanStatus.Activo)
                {
                    return ValidationResult<EditAnnualInterestRateDto>.Failure(LoandError.LoanIsNotActive);
                }

                var dto = new EditAnnualInterestRateDto
                {
                    Id = loan.Id,
                    AnnualInterestRate = loan.AnnualInterestRate
                };

                return ValidationResult<EditAnnualInterestRateDto>.Success(dto);
            }
            catch (Exception)
            {
                return ValidationResult<EditAnnualInterestRateDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<LoanRateUpdatedDto>> EditAnnualInterestRateAsync(
            EditAnnualInterestRateDto dto, string adminUserId)
        {
            if (dto.AnnualInterestRate < 0m)
            {
                return ValidationResult<LoanRateUpdatedDto>.Failure(LoandError.NegativeAnnualInterestRate);
            }

            try
            {
                var loan = await _loansRepository.GetLoanWithInstallmentsAsync(dto.Id);
                if (loan is null)
                {
                    return ValidationResult<LoanRateUpdatedDto>.Failure(LoandError.NonExistsLoan);
                }

                if (loan.Status != LoanStatus.Activo)
                {
                    return ValidationResult<LoanRateUpdatedDto>.Failure(LoandError.LoanIsNotActive);
                }

                var today = DateTimeOffset.UtcNow;
                var futureInstallments = loan.loanInstallments
                    .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                    .ToList();

                if (futureInstallments.Count == 0)
                {
                    return ValidationResult<LoanRateUpdatedDto>.Failure(LoandError.NonExistsFutureInstallments);
                }

                //Solo se redistribuye el capital que aún no ha sido amortizado
                var pendingCapital = futureInstallments.Sum(i => i.CapitalAmount);

                var recalculated = _amortizationCalculator.RecalculateFutureInstallments(
                    loan.loanInstallments.ToList(),
                    pendingCapital,
                    dto.AnnualInterestRate,
                    today,
                    adminUserId);

                loan.AnnualInterestRate = dto.AnnualInterestRate;
                loan.MonthlyInstallment = recalculated[0].InstallmentValue;
                loan.TotalPayable = loan.loanInstallments.Sum(i => i.InstallmentValue);
                loan.PendingAmount = loan.loanInstallments
                    .Where(i => i.paymentStatus != PaymentStatus.Pagada)
                    .Sum(i => i.PendingBalance);
                loan.ModifiedAt = today;
                loan.LastModifiedByIdUser = adminUserId;

                await _loansRepository.UpdateAsync(loan);
                await _loanInstallmentRepository.UpdateRangeLoansInstallmentAsync(recalculated.ToList());

                //Tasa y cuotas futuras se confirman juntas o no se confirma ninguna
                await _loansRepository.SaveChangesAsync();

                var nextInstallment = recalculated[0];

                var rateUpdated = new LoanRateUpdatedDto
                {
                    CustomerId = loan.CustomerId,
                    LoanNumber = loan.LoanNumber,
                    AnnualInterestRate = loan.AnnualInterestRate,
                    NextInstallmentValue = nextInstallment.InstallmentValue,
                    NextInstallmentDueDate = nextInstallment.DueDate
                };

                return ValidationResult<LoanRateUpdatedDto>.Success(rateUpdated);
            }
            catch (Exception)
            {
                return ValidationResult<LoanRateUpdatedDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult> SendRateUpdateNotificationAsync(
            LoanRateUpdatedDto rateUpdated, string customerEmail, string customerFullName)
        {
            var message = new MessageDto
            {
                To = customerEmail,
                Subject = "Actualización de tasa de interés de préstamo",
                Message = BuildRateUpdateBody(rateUpdated, customerFullName)
            };

            var sent = await _emailServices.SendNotification(message);

            return sent
                ? ValidationResult.Success()
                : ValidationResult.Failure(LoandError.RateUpdatedWithoutNotification);
        }

        private static string BuildRateUpdateBody(LoanRateUpdatedDto rateUpdated, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               $"<p>La tasa de interés de su préstamo {rateUpdated.LoanNumber} ha sido actualizada.</p>" +
               $"<p>Nueva tasa de interés anual: {rateUpdated.AnnualInterestRate}%<br/>" +
               $"Nuevo valor de la próxima cuota: RD${rateUpdated.NextInstallmentValue:N2}<br/>" +
               $"Fecha de vencimiento de la próxima cuota: {rateUpdated.NextInstallmentDueDate:dd/MM/yyyy}</p>" +
               "<p>Esta modificación aplica únicamente a las cuotas futuras pendientes.</p>";

        private static LoanStatus? ToLoanStatus(LoanStatusFilter filter)
            => filter switch
            {
                LoanStatusFilter.Activos => LoanStatus.Activo,
                LoanStatusFilter.Completados => LoanStatus.Completado,
                _ => null
            };
    }
}
