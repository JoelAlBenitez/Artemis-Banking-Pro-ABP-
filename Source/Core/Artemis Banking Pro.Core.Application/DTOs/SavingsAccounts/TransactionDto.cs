using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    //Proyección del historial mostrado en el detalle de la cuenta. La entidad Transaction y
    //su repositorio pertenecen al módulo Cliente; aquí solo vive el contrato de lectura.
    public sealed class TransactionDto
    {
        public required DateTimeOffset TransactionDate { get; set; }
        public required decimal Amount { get; set; }
        public required TransactionType TypeTransaction { get; set; }

        //Destino de la transacción: número de cuenta beneficiaria, RETIRO, últimos 4 dígitos
        //de la tarjeta pagada o número del préstamo pagado.
        public required string Beneficiary { get; set; }

        //Fuente de la transacción: número de cuenta de origen, últimos 4 dígitos de la tarjeta
        //del avance, número del préstamo desembolsado o DEPÓSITO.
        public required string Origin { get; set; }

        public required TransactionStatus StateTransaction { get; set; }
    }
}
