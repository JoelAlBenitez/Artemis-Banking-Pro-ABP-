namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    //Único punto del sistema autorizado a manipular el CVC en claro.
    public interface ICvcHasher
    {
        string GenerateCvc();
        string Hash(string cvc);
        bool Verify(string cvc, string cvcHash);
    }
}
