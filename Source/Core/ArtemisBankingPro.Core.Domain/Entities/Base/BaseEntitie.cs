namespace ArtemisBankingPro.Core.Domain.Entities.Base
{
    public abstract class BaseEntitie <Tkey>
    {
        public Tkey? Id { get; set; }
        public required DateTimeOffset CreatedAt { get; set; } 
        public required string CreateByUserId { get; set; }
        public string? LastModifiedByIdUser { get; set; }   
        public DateTimeOffset? ModifiedAt { get; set; }
    }
}
