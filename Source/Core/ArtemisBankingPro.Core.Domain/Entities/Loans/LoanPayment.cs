using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;

namespace ArtemisBankingPro.Core.Domain.Entities.Loans
{
    //Trazabilidad del pago de una cuota. Los pagos los originan los módulos Cliente y Cajero;
    //el administrador no paga cuotas. Vive aquí porque forma parte del agregado del préstamo.
    public sealed class LoanPayment : BaseEntitie<int>
    {
        public required int LoandId { get; set; }
        public required int LoanInstallmentId {  get; set; }
        public required DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;

        //Monto digitado por el usuario antes de acotarlo contra el pendiente real de la cuota
        public required decimal RequestedAmount { get; set; }
        public required decimal EffectiveAmount { get; set; }
        public required ChannelPayment Channel {  get; set; }
        public required string PerformedByUserId { get; set; }

        //Movimiento de DÉBITO que originó el pago. Queda nulo hasta que la transacción recibe su
        //identidad: ambos registros se confirman en el mismo SaveChangesAsync por la navegación.
        public int? TransactionId { get; set; }

        public  Loan? Loans { get; set; }
        public LoanInstallment? loanInstallment { get; set; }
        public Transaction? Transaction { get; set; }
    }
}
