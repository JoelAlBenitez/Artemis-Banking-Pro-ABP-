namespace Artemis_Banking_Pro.Core.Application.DTOs.Commerces
{
    /// <summary>
    /// Comercio en el listado paginado.
    /// </summary>
    public sealed class CommerceListItemDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Rnc { get; set; }
        public bool IsActive { get; set; }
        public bool HasAssociatedUser { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Detalle de un comercio con su usuario asociado.
    /// </summary>
    public sealed class CommerceDetailDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Rnc { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        //Vive en Identity: el handler lo compone a partir de AssociatedUserId
        public CommerceAssociatedUserDto? AssociatedUser { get; set; }
    }

    public sealed class CommerceAssociatedUserDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public bool IsActive { get; set; }
    }
}
