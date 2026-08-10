using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ArtemisBankingPro.Infraestructrue.Shared.Services.Generators
{
    public sealed class CardNumberGenerator : ICardNumberGenerator
    {
        private const int MaxGenerationAttempts = 10;

        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ILogger<CardNumberGenerator> _logger;

        public CardNumberGenerator(
            ICreditCardsRepository creditCardsRepository,
            ILogger<CardNumberGenerator> logger)
        {
            _creditCardsRepository = creditCardsRepository;
            _logger = logger;
        }

        public async Task<string?> GenerateUniqueCardNumberAsync()
        {
            for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
            {
                var candidate = BuildCandidate();

                var exists = await _creditCardsRepository
                    .ExistElementByConsult(card => card.CardNumber == candidate);

                if (!exists)
                {
                    return candidate;
                }

                _logger.LogWarning("El número de tarjeta generado ya se encuentra registrado. Intento {Intento} de {Maximo}",
                    attempt, MaxGenerationAttempts);
            }

            _logger.LogError("Se agotaron los {Maximo} intentos de generación de un número de tarjeta único",
                MaxGenerationAttempts);

            return null;
        }

        //Se construye dígito a dígito y se persiste como texto para preservar los ceros iniciales.
        private static string BuildCandidate()
        {
            var digits = new char[DomainConstants.CardNumberLength];

            for (var position = 0; position < digits.Length; position++)
            {
                digits[position] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
            }

            return new string(digits);
        }
    }
}
