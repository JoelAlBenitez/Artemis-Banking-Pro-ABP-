namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    public class ClientSummaryDto
    {
        public required string IDCARD { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
    }
}
