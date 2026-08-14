using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;

namespace ArtemisBankingPro.Core.Domain.Entities.Commerces
{
    public sealed class Commerce : BaseEntitie<int>
    {
        public required string Name { get; set; }

        public string? Description { get; set; }

        public required string Email { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Rnc { get; set; }

        public required CommerceStatus Status { get; set; }

        //Usuario con rol Comercio en Identity. Referencia lógica sin FK física entre contextos.
        //Un comercio admite un solo usuario asociado.
        public string? AssociatedUserId { get; set; }

        public bool IsActive => Status == CommerceStatus.Activo;

        public bool HasAssociatedUser => !string.IsNullOrWhiteSpace(AssociatedUserId);

        public ICollection<CommercePayment> Payments { get; set; } = new List<CommercePayment>();
    }
}
