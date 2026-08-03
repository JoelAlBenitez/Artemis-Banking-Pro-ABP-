using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.CreditCards
{
    //El historial de consumos se consulta con los miembros del repositorio genérico:
    //GetAllAsync cubre el paginado, el filtro por tarjeta y el orden descendente por fecha.
    public interface ICardConsumptionRepository :
        IGenericRepository<CardConsumption, int>
    {
    }
}
