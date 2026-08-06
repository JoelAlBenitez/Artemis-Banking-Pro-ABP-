namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    //Datos del préstamo aprobado que viajan al correo de notificación (pág. 43).
    public sealed class LoanAssignedDto
    {
        public required string CustomerId { get; set; }
        public required string LoanNumber { get; set; }
        public required decimal ApprovedAmount { get; set; }
        public required int Term { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required decimal MonthlyInstallment { get; set; }
    }
}
