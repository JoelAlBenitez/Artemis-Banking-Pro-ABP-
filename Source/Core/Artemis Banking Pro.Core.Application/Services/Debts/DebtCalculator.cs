using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Debts
{
    public sealed class DebtCalculator : IDebtCalculator
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ILogger<DebtCalculator> _logger;

        public DebtCalculator(
            ILoansRepository loansRepository,
            ICreditCardsRepository creditCardsRepository,
            ILogger<DebtCalculator> logger)
        {
            _loansRepository = loansRepository;
            _creditCardsRepository = creditCardsRepository;
            _logger = logger;
        }

        public async Task<decimal> GetCustomerDebtAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return 0m;

            var loansDebt = await _loansRepository.SumAsync(
                loan => loan.CustomerId == customerId && loan.Status == LoanStatus.Activo,
                loan => loan.PendingAmount);

            var cardsDebt = await _creditCardsRepository.SumAsync(
                card => card.CustomerId == customerId && card.Status == CreditCardStatus.Activa,
                card => card.OwedAmount);

            var debt = loansDebt + cardsDebt;

            _logger.LogInformation("Deuda actual del cliente {CustomerId}: RD${Deuda}", customerId, debt);

            return debt;
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetCustomersDebtAsync(
            IReadOnlyCollection<string> customerIds)
        {
            var debts = new Dictionary<string, decimal>();
            if (customerIds is null || customerIds.Count == 0) return debts;

            var activeLoans = await _loansRepository.GetAllFindAsync(loan => loan.Status == LoanStatus.Activo);
            var activeCards = await _creditCardsRepository.GetAllFindAsync(card => card.Status == CreditCardStatus.Activa);

            foreach (var customerId in customerIds.Distinct())
            {
                debts[customerId] =
                    activeLoans.Where(loan => loan.CustomerId == customerId).Sum(loan => loan.PendingAmount)
                    + activeCards.Where(card => card.CustomerId == customerId).Sum(card => card.OwedAmount);
            }

            return debts;
        }

        public async Task<decimal> GetAverageDebtAsync(IReadOnlyCollection<string>? activeCustomerIds = null)
        {
            var activeLoans = await _loansRepository.GetAllFindAsync(loan => loan.Status == LoanStatus.Activo);
            var activeCards = await _creditCardsRepository.GetAllFindAsync(card => card.Status == CreditCardStatus.Activa);

            //La lista de clientes activos proviene del project Identity. Mientras su consulta no
            //esté disponible, el promedio se calcula sobre los clientes que hoy tienen algún
            //producto financiero activo: el divisor es el único dato que queda pendiente.
            var customerIds = activeCustomerIds is { Count: > 0 }
                ? activeCustomerIds.Distinct().ToList()
                : activeLoans.Select(loan => loan.CustomerId)
                    .Concat(activeCards.Select(card => card.CustomerId))
                    .Distinct()
                    .ToList();

            if (customerIds.Count == 0)
            {
                _logger.LogWarning("No existen clientes activos para calcular la deuda promedio. Se asume RD$0.00");
                return 0m;
            }

            var totalDebt =
                activeLoans.Where(loan => customerIds.Contains(loan.CustomerId)).Sum(loan => loan.PendingAmount)
                + activeCards.Where(card => customerIds.Contains(card.CustomerId)).Sum(card => card.OwedAmount);

            var average = Math.Round(totalDebt / customerIds.Count, 2, MidpointRounding.AwayFromZero);

            _logger.LogInformation("Deuda promedio del sistema: RD${Promedio} sobre {Clientes} clientes activos",
                average, customerIds.Count);

            return average;
        }

        public decimal GetProjectedDebt(decimal currentDebt, decimal totalPayableOfNewLoan)
            => currentDebt + totalPayableOfNewLoan;
    }
}
