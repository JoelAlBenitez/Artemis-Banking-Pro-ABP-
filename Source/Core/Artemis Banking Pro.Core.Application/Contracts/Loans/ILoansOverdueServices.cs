using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    //Control automático de cuotas atrasadas. Lo consume el proceso diario (Azure Function).
    public interface ILoansOverdueServices
    {
        Task<ValidationResult<OverdueInstallmentsResultDto>> ReviewOverdueInstallmentsAsync();
    }
}
