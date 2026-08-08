using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{

    public interface ILoansOverdueServices
    {
        Task<ValidationResult<OverdueInstallmentsResultDto>> ReviewOverdueInstallmentsAsync();
    }
}
