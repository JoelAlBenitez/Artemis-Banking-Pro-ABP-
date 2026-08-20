namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    /// <summary>
    /// Usuario en los listados paginados de la Web API.
    /// </summary>
    public class UserListItemDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Identification { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Usuario con rol Comercio, con el comercio al que está asociado.
    /// </summary>
    public class CommerceUserListItemDto : UserListItemDto
    {
        public int? CommerceId { get; set; }
        public string? CommerceName { get; set; }
    }

    /// <summary>
    /// Usuario recién creado. Siempre queda inactivo hasta confirmar la cuenta.
    /// </summary>
    public class UserCreatedDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Usuario de comercio recién creado.
    /// </summary>
    public class CommerceUserCreatedDto : UserCreatedDto
    {
        public int CommerceId { get; set; }
    }

    /// <summary>
    /// Detalle de un usuario con su cuenta de ahorro principal.
    /// </summary>
    public class UserApiDetailDto : UserListItemDto
    {
        public DateTimeOffset CreatedAt { get; set; }

        //Vive en Persistence mientras el usuario vive en Identity: el handler los compone
        public MainAccountDto? MainAccount { get; set; }
    }

    public class MainAccountDto
    {
        public required string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public bool IsPrincipal { get; set; }
        public required string Status { get; set; }
    }
}
