namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm
{
    public class AtmDepositDto
    {
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CashierId { get; set; } = string.Empty;
    }

    public class AtmWithdrawalDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CashierId { get; set; } = string.Empty;
    }

    public class AtmCreditCardPaymentDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string CreditCardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CashierId { get; set; } = string.Empty;
    }

    public class AtmLoanPaymentDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CashierId { get; set; } = string.Empty;
    }

    public class AtmThirdPartyTransferDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CashierId { get; set; } = string.Empty;
    }

    public class AtmIndicatorsDto
    {
        public int TotalTransactions { get; set; }
        public int TotalPayments { get; set; }
        public int TotalDeposits { get; set; }
        public int TotalWithdrawals { get; set; }
    }

    public class AtmAccountDetailsDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
    }

    public class AtmCreditCardDetailsDto
    {
        public string CreditCardNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Debt { get; set; }
    }

    public class AtmLoanDetailsDto
    {
        public string LoanNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal PendingAmount { get; set; }
        public bool HasPendingInstallments { get; set; }
    }
}
