namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CreditCardAssignmentDto
    {
        //Identificador del cliente en Identity: texto, sin FK física
        public required string CustomerId { get; set; }
        public required decimal CreditLimit { get; set; }
    }
}
