using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace ArtemisBankingPro.Unit.Tests.Services.CreditCards
{
    public sealed class TempDuplicateProbe
    {
        private readonly ITestOutputHelper _output;

        public TempDuplicateProbe(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Probe()
        {
            var configuration = new MapperConfiguration(
                expression => expression.AddMaps(typeof(CreditCardsMappingEntitieToDtoAndReverse).Assembly),
                NullLoggerFactory.Instance);

            var mapper = configuration.CreateMapper();

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "c1",
                CreditLimit = 1000m,
                OwedAmount = 250m,
                ExpirationDate = new DateTimeOffset(2029, 5, 1, 0, 0, 0, TimeSpan.Zero),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var dto = mapper.Map<CreditCardDto>(card);

            _output.WriteLine($"Masked={dto.MaskedCardNumber}");
            _output.WriteLine($"Available={dto.AvailableCredit}");
            _output.WriteLine($"Expiration={dto.ExpirationDate}");

            Assert.True(false, $"Available={dto.AvailableCredit}; Masked={dto.MaskedCardNumber}; Exp={dto.ExpirationDate}");
        }
    }
}
