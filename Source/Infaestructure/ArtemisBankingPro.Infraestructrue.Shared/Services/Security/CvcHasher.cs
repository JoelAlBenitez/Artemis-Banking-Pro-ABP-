using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using System.Security.Cryptography;
using System.Text;

namespace ArtemisBankingPro.Infraestructrue.Shared.Services.Security
{
    public sealed class CvcHasher : ICvcHasher
    {
        //El CVC en claro solo existe dentro de esta clase: nunca se persiste, se expone ni se registra.
        public string GenerateCvc()
        {
            var upperBound = (int)Math.Pow(10, DomainConstants.CvcLength);
            var cvc = RandomNumberGenerator.GetInt32(0, upperBound);

            return cvc.ToString().PadLeft(DomainConstants.CvcLength, '0');
        }

        public string Hash(string cvc)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(cvc));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public bool Verify(string cvc, string cvcHash)
        {
            if (string.IsNullOrWhiteSpace(cvc) || string.IsNullOrWhiteSpace(cvcHash))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hash(cvc)),
                Encoding.UTF8.GetBytes(cvcHash.ToLowerInvariant()));
        }
    }
}
