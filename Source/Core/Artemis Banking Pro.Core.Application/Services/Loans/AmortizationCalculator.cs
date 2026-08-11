using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
    public sealed class AmortizationCalculator : 
        IAmortizationCalculator
    {
        private const int MoneyDecimals = 2;

        private readonly ILogger<AmortizationCalculator> _logger;
        public AmortizationCalculator(ILogger<AmortizationCalculator> logger)
        {
            _logger = logger;
        }

        public decimal CalculateMonthlyInstallment(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments)
        {
            _logger.LogInformation("Realizando calculo de la cutoas seg�n la tarifa determinada.");
            if (totalInstallments <= 0) return 0m;
            var monthlyRate = MonthlyRate(annualInterestRate);
            if (monthlyRate == 0m) return Money(capital / totalInstallments);
            var compound = Compound(monthlyRate, totalInstallments);
            return Money(capital * (monthlyRate * compound) / (compound - 1m));
        }

        public IReadOnlyList<LoanInstallment> GenerateAmortizationTable(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments,
            DateTimeOffset loanCreatedAt,
            int loanId,
            string createdByUserId)
        {
            var installments = new List<LoanInstallment>();
            if (totalInstallments <= 0) return installments;

            #region  calculando la tarifa mensual de as cuotas
            _logger.LogInformation("Realizaci�n del calculo de la tarifa mensual" +
                " y las mensualidades de las cuotas del prestamo con ID {ID} ", loanId);
            var monthlyRate = MonthlyRate(annualInterestRate);
            var monthlyInstallment = CalculateMonthlyInstallment(capital, annualInterestRate, totalInstallments);
            var pendingCapital = capital;

            #endregion

            #region  creando los objectos de las cutoas asignadas a un determinado prestamo
            _logger.LogInformation("Creando las cuotas del prestamo con ID {ID}", loanId);
            for (var number = 1; number <= totalInstallments; number++)
            {
                var interest = Money(pendingCapital * monthlyRate);
                var capitalAmount = Money(monthlyInstallment - interest);
                var installmentValue = monthlyInstallment;

                if (number == totalInstallments)
                {
                    capitalAmount = pendingCapital;
                    installmentValue = Money(capitalAmount + interest);
                }

                pendingCapital = Money(pendingCapital - capitalAmount);

                _logger.LogInformation("Creando cuota determinada n�mero {N} al prestamo con ID {ID}", number, loanId);
                installments.Add(new LoanInstallment
                {
                    LoanId = loanId,
                    InstallmentNumber = number,
                    DueDate = loanCreatedAt.AddMonths(number),
                    InstallmentValue = installmentValue,
                    InterestAmount = interest,
                    CapitalAmount = capitalAmount,
                    PendingBalance = installmentValue,
                    paymentStatus = PaymentStatus.Pendiente,
                    IsOverdue = false,
                    CreatedAt = loanCreatedAt,
                    CreateByUserId = createdByUserId
                });

            }
            #endregion

            return installments;
        }

        public IReadOnlyList<LoanInstallment> RecalculateFutureInstallments(
            IReadOnlyCollection<LoanInstallment> installments,
            decimal pendingCapital,
            decimal newAnnualInterestRate,
            DateTimeOffset today,
            string modifiedByUserId
           )
        {
            #region obteniendo cuotas futuras
            _logger.LogInformation("Recuperando cuotas con pago pendientes del prestamos determinado cuya fecha de pago sea futura.");
            var futureInstallments = installments
                .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            if (futureInstallments.Count == 0) return futureInstallments;
            #endregion

            #region calculo en base a nueva tarifa
            _logger.LogInformation("Realizando calculos de las tarifas de las nuevas cuotas");
            var monthlyRate = MonthlyRate(newAnnualInterestRate);
            var monthlyInstallment = CalculateMonthlyInstallment(
                pendingCapital, newAnnualInterestRate, futureInstallments.Count);

            #endregion
            #region Cuotas futuras modificacion
            _logger.LogInformation("Modificando cuotas futuras del prestamo.");
            for (var index = 0; index < futureInstallments.Count; index++)
            {
                var installment = futureInstallments[index];

                var interest = Money(pendingCapital * monthlyRate);
                var capitalAmount = Money(monthlyInstallment - interest);
                var installmentValue = monthlyInstallment;

                if (index == futureInstallments.Count - 1)
                {
                    capitalAmount = pendingCapital;
                    installmentValue = Money(capitalAmount + interest);
                }

                pendingCapital = Money(pendingCapital - capitalAmount);
                installment.InstallmentValue = installmentValue;
                installment.InterestAmount = interest;
                installment.CapitalAmount = capitalAmount;
                installment.PendingBalance = installmentValue;
                installment.ModifiedAt = today;
                installment.LastModifiedByIdUser = modifiedByUserId;
            }
            #endregion

            return futureInstallments;
        }
        #region private methos

        private static decimal MonthlyRate(decimal annualInterestRate)
            => annualInterestRate <= 0m ? 0m : annualInterestRate / 100m / 12m;

        private static decimal Compound(decimal monthlyRate, int totalInstallments)
        {
            var compound = 1m;
            for (var i = 0; i < totalInstallments; i++) compound *= 1m + monthlyRate;
            return compound;
        }

        private static decimal Money(decimal value)
            => Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
        #endregion

    }
}
