namespace ArtemisBankingPro.Core.Domain.Entities.Base
{
    public abstract class BaseEntitie <Tkey>
    {
        public Tkey? Id { get; set; }
        //representa el user que realizo la accion -> admin, cliente...
        public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        //administrador responsable de la accion de asignaciones o creaciones
        public required string CreateByUserId { get; set; }
        //quien modifico, admin
        public string? LastModifiedByIdUser { get; set; }   
        public DateTimeOffset? ModifiedAt { get; set; }
    }
}
