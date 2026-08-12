using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Commerces
{
    public interface IHermesPayServices
    {
        //Procesa el pago de forma transaccional. Un consumo rechazado por falta de crédito
        //queda registrado sin modificar balances ni deudas.
        Task<ValidationResult> ProcessPaymentAsync(Commerce commerce, ProcessPaymentDto dto);
    }
}
