namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class ClientDto
    {
        public required string Id { get; set; }
        public required string IdCard { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required bool IsActive { get; set; }
    }
}
