namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    public sealed class SavingsAccountAssignmentDto
    {
        //Identificador del cliente en Identity: texto, sin FK física
        public required string CustomerId { get; set; }

        //Puede ser RD$0.00, pero nunca negativo. El número de cuenta y el tipo
        //(siempre Secundaria) los asigna el sistema.
        public required decimal InitialBalance { get; set; }
    }
}
