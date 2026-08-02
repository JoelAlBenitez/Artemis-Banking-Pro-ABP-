using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ArtemisBankingPro.Infraestructrue.Shared.Services.Generators
{
    //Las cuentas de ahorro y los préstamos comparten el mismo espacio de numeración de 9
    //dígitos: un número emitido aquí no puede existir como cuenta ni como préstamo.
    public sealed class AccountNumberGenerator : IAccountNumberGenerator
    {
        private const int MaxGenerationAttempts = 10;

        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly ILogger<AccountNumberGenerator> _logger;

        public AccountNumberGenerator(
            ISavingsAccountsRepository savingsAccountsRepository,
            ILoansRepository loansRepository,
            ILogger<AccountNumberGenerator> logger)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
            _loansRepository = loansRepository;
            _logger = logger;
        }

        public async Task<string?> GenerateUniqueAccountNumberAsync()
        {
            for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
            {
                var candidate = BuildCandidate();

                var existsAsAccount = await _savingsAccountsRepository.ExistsAccountNumberAsync(candidate);
                var existsAsLoan = await _loansRepository
                    .ExistElementByConsult(loan => loan.LoanNumber == candidate);

                if (!existsAsAccount && !existsAsLoan)
                {
                    return candidate;
                }

                _logger.LogWarning("El número de cuenta generado ya se encuentra registrado. Intento {Intento} de {Maximo}",
                    attempt, MaxGenerationAttempts);
            }

            _logger.LogError("Se agotaron los {Maximo} intentos de generación de un número de cuenta único",
                MaxGenerationAttempts);

            return null;
        }

        //Se construye dígito a dígito y se persiste como texto para preservar los ceros iniciales.
        private static string BuildCandidate()
        {
            var digits = new char[DomainConstants.AccountNumberLength];

            for (var position = 0; position < digits.Length; position++)
            {
                digits[position] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
            }

            return new string(digits);
        }
    }
}
