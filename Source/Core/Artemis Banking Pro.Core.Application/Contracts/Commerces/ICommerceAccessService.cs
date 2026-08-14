using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Commerces
{
    //Regla transversal a los dos endpoints de Hermes Pay: el comercio efectivo depende del rol
    //autenticado. Un Administrador opera sobre el commerceId de la URL; un Comercio solo sobre
    //el suyo, y el valor de la URL se ignora.
    public interface ICommerceAccessService
    {
        Task<ValidationResult<Commerce>> ResolveCommerceAsync(int commerceIdFromRoute);
    }
}
