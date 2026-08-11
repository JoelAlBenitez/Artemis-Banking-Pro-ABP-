namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    /// <summary>
    /// Préstamo en el listado paginado de la Web API.
    /// </summary>
    public sealed class LoanListItemDto
    {
        public int Id { get; set; }
        public required string LoanNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public decimal CapitalAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public required string Status { get; set; }

        //Al día o en mora
        public required string ClientPaymentStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Préstamo recién asignado con su cuota mensual y el total a pagar.
    /// </summary>
    public sealed class LoanCreatedDto
    {
        public int Id { get; set; }
        public required string LoanNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal TotalAmountToPay { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Cuota de la tabla de amortización.
    /// </summary>
    public sealed class LoanInstallmentApiDto
    {
        public int InstallmentNumber { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal PendingInstallmentAmount { get; set; }
        public required string PaymentStatus { get; set; }
        public bool IsLate { get; set; }
    }

    /// <summary>
    /// Detalle de un préstamo con su tabla de amortización.
    /// </summary>
    public sealed class LoanDetailApiDto
    {
        public int Id { get; set; }
        public required string LoanNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientFullName { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal PendingAmount { get; set; }
        public required string Status { get; set; }
        public string ClientPaymentStatus { get; set; } = string.Empty;
        public IReadOnlyCollection<LoanInstallmentApiDto> Amortization { get; set; } = [];
    }

    /// <summary>
    /// Resultado de asignar un préstamo: el préstamo creado, o el conflicto de alto riesgo
    /// que el administrador debe confirmar antes de continuar.
    /// </summary>
    public sealed class LoanAssignmentResultDto
    {
        public LoanCreatedDto? Loan { get; set; }
        public HighRiskConflictDto? HighRisk { get; set; }
    }

    /// <summary>
    /// Cuerpo del 409 Conflict cuando el cliente es o se convierte en cliente de alto riesgo.
    /// </summary>
    public sealed class HighRiskConflictDto
    {
        public required string Message { get; set; }
        public required string RiskType { get; set; }
        public decimal CurrentDebt { get; set; }
        public decimal ProjectedDebt { get; set; }
        public decimal AverageDebt { get; set; }
    }
}
