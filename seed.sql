SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @UserId NVARCHAR(450) = '17e67000-c214-4b38-b2d4-bdd36e6c6988';
DECLARE @Now DATETIME2 = GETDATE();

-- Delete previous dummy data
DELETE FROM Transactions;
DELETE FROM SavingsAccounts;
DELETE FROM CreditCards;
DELETE FROM Loans;

-- Seed Savings Accounts (Status 1 = Active)
INSERT INTO SavingsAccounts (AccountNumber, CustomerId, Balance, AccountType, Status, CreatedAt, CreateByUserId)
VALUES ('001122334', @UserId, 50000.00, 0, 1, @Now, 'system');

INSERT INTO SavingsAccounts (AccountNumber, CustomerId, Balance, AccountType, Status, CreatedAt, CreateByUserId)
VALUES ('998877665', @UserId, 15000.00, 1, 1, @Now, 'system');

-- Seed Credit Cards (Status 1 = Active)
INSERT INTO CreditCards (CardNumber, LastFourDigits, CustomerId, CreditLimit, OwedAmount, ExpirationDate, CvcHash, Status, CreatedAt, CreateByUserId, AssignedByAdminId)
VALUES ('1234567890123456', '3456', @UserId, 100000.00, 15000.00, DATEADD(year, 4, @Now), 'dummy_hash', 1, @Now, 'system', @UserId);

-- Seed Loans (Status 1 = Active)
INSERT INTO Loans (LoanNumber, CustomerId, ApprovedCapital, termMonths, AnnualInterestRate, MonthlyInstallment, TotalPayable, PendingAmount, Status, CreatedAt, CreateByUserId)
VALUES ('L-001', @UserId, 200000.00, 36, 12.00, 6642.00, 239112.00, 150000.00, 1, @Now, 'system');

-- Seed a Transaction for the Savings Account so ATM sees something
DECLARE @AccountId INT = (SELECT TOP 1 Id FROM SavingsAccounts WHERE AccountNumber = '001122334');

-- TransactionType 0 (Deposit), OperationType 1 (Credit), Status 2 (Completed)
INSERT INTO Transactions (SavingsAccountId, Amount, TransactionType, OperationType, Origin, Beneficiary, Status, CreatedAt, CreateByUserId, PerformedByUserId, Channel)
VALUES (@AccountId, 5000.00, 0, 1, 'Deposito Inicial', 'clienteuser', 2, @Now, 'system', @UserId, 0);
