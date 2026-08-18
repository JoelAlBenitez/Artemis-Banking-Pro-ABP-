SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Prueba1Id NVARCHAR(450) = '429a66f5-13db-41b3-9860-2bd5c927c01e';
DECLARE @Prueba2Id NVARCHAR(450) = 'b876c001-a751-4441-8edd-b8f12d7e2c6d';
DECLARE @Now DATETIME2 = GETDATE();

-- PRUEBA 1 DATA
-- Seed Savings Accounts
INSERT INTO SavingsAccounts (AccountNumber, CustomerId, Balance, AccountType, Status, CreatedAt, CreateByUserId)
VALUES ('111000111', @Prueba1Id, 80000.00, 0, 1, @Now, 'system');

INSERT INTO SavingsAccounts (AccountNumber, CustomerId, Balance, AccountType, Status, CreatedAt, CreateByUserId)
VALUES ('222000222', @Prueba1Id, 12000.00, 1, 1, @Now, 'system');

-- Seed Credit Cards
INSERT INTO CreditCards (CardNumber, LastFourDigits, CustomerId, CreditLimit, OwedAmount, ExpirationDate, CvcHash, Status, CreatedAt, CreateByUserId, AssignedByAdminId)
VALUES ('1111222233334444', '4444', @Prueba1Id, 50000.00, 5000.00, DATEADD(year, 3, @Now), 'dummy_hash', 1, @Now, 'system', @Prueba1Id);

-- Seed Loans
INSERT INTO Loans (LoanNumber, CustomerId, ApprovedCapital, termMonths, AnnualInterestRate, MonthlyInstallment, TotalPayable, PendingAmount, Status, CreatedAt, CreateByUserId)
VALUES ('L-002', @Prueba1Id, 100000.00, 24, 10.00, 4614.00, 110736.00, 50000.00, 1, @Now, 'system');


-- PRUEBA 2 DATA
-- Seed Savings Accounts
INSERT INTO SavingsAccounts (AccountNumber, CustomerId, Balance, AccountType, Status, CreatedAt, CreateByUserId)
VALUES ('333000333', @Prueba2Id, 5000.00, 0, 1, @Now, 'system');

-- Seed Credit Cards
INSERT INTO CreditCards (CardNumber, LastFourDigits, CustomerId, CreditLimit, OwedAmount, ExpirationDate, CvcHash, Status, CreatedAt, CreateByUserId, AssignedByAdminId)
VALUES ('5555666677778888', '8888', @Prueba2Id, 25000.00, 24000.00, DATEADD(year, 2, @Now), 'dummy_hash', 1, @Now, 'system', @Prueba2Id);

-- Add transaction for Prueba 1
DECLARE @AccountId1 INT = (SELECT TOP 1 Id FROM SavingsAccounts WHERE AccountNumber = '111000111');
INSERT INTO Transactions (SavingsAccountId, Amount, TransactionType, OperationType, Origin, Beneficiary, Status, CreatedAt, CreateByUserId, PerformedByUserId, Channel)
VALUES (@AccountId1, 1000.00, 0, 1, 'Deposito Nomina', 'prueba1', 2, @Now, 'system', @Prueba1Id, 0);

-- Add transaction for Prueba 2
DECLARE @AccountId2 INT = (SELECT TOP 1 Id FROM SavingsAccounts WHERE AccountNumber = '333000333');
INSERT INTO Transactions (SavingsAccountId, Amount, TransactionType, OperationType, Origin, Beneficiary, Status, CreatedAt, CreateByUserId, PerformedByUserId, Channel)
VALUES (@AccountId2, 500.00, 1, 2, 'Pago Servicios', 'Claro', 2, @Now, 'system', @Prueba2Id, 0);

