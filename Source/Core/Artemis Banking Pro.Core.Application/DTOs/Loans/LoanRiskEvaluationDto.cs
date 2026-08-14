using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    
    public sealed class LoanRiskEvaluationDto
    {
        public required LoanRiskType RiskType { get; set; }
        public required string Message { get; set; }
        public required decimal CurrentDebt { get; set; }
        public required decimal ProjectedDebt { get; set; }
        public required decimal AverageDebt { get; set; }

        public bool RequiresConfirmation => RiskType != LoanRiskType.SinRiesgo;
    }
}
