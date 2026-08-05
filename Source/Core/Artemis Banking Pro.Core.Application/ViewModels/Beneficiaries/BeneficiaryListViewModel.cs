namespace Artemis_Banking_Pro.Core.Application.ViewModels.Beneficiaries
{
    public class BeneficiaryListViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string AccountNumber { get; set; }
    }
}
