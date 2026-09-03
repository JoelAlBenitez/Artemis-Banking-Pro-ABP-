using Artemis_Banking_Pro.Core.Application.Contracts.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.AdminDashboard
{
    //Indicadores generales del Home del administrador. Solo lectura: no escribe nada.
    public sealed class AdminDashboardServices : IAdminDashboardServices
    {
        private readonly ILogger<AdminDashboardServices> _logger;
        private readonly ILoansRepository _loansRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IDebtCalculator _debtCalculator;

        public AdminDashboardServices(ILogger<AdminDashboardServices> logger,
            ILoansRepository loansRepository,
            ISavingsAccountsRepository savings,
            ICreditCardsRepository creditCardsRepository,
            ITransactionRepository transactionRepository,
            IUserManagementService userManagementService,
            IDebtCalculator debtCalculator
            )
        {
            _creditCardsRepository = creditCardsRepository;
            _loansRepository = loansRepository;
            _savingsAccountsRepository = savings;
            _transactionRepository = transactionRepository;
            _userManagementService = userManagementService;
            _debtCalculator = debtCalculator;
            _logger = logger;
        }

        public async Task<ValidationResult<AdminDashboardDto>> GetDataAdminDashboard()
        {
            try
            {
                _logger.LogInformation("Recuperando los indicadores generales del Home del administrador");

                //Indicadores 1 y 2: toda operación que genere registro de transacción.
                //El "del día" usa la fecha de creación de la transacción.
                var totalHistoricalTransactions = await _transactionRepository.GetTotalHistoricalAsync();
                var dayTransactions = await _transactionRepository.GetTotalTodayAsync();

                //Indicadores 3 y 4: pago es solo lo que salda una obligación financiera
                //(tarjeta o préstamo) y quedó aprobado. El repositorio es el único lugar donde
                //vive esa definición: depósitos, retiros, transferencias y avances no cuentan.
                var historicalPayments = await _transactionRepository.GetPaymentsAsync(null, null);
                var dayPayments = await _transactionRepository.GetPaymentsAsync(null, DateTimeOffset.UtcNow);

                //Indicadores 5 y 6: el estado de los clientes vive en Identity, no en el dominio.
                var activeClients = await _userManagementService.GetActiveClientsAsync();
                var allClients = await _userManagementService.GetUsersByRoleAsync(Roles.Cliente, 1, 1);

                var customerActive = activeClients.Count;

                //Identity no expone un conteo de clientes inactivos. TotalCount del listado por
                //rol sí trae todos los clientes sin importar su estado: el resto son inactivos.
                var customerInactive = Math.Max(allClients.TotalCount - customerActive, 0);

                //Indicadores 8, 9 y 10: solo productos activos. Los préstamos completados, las
                //tarjetas canceladas y las cuentas canceladas quedan fuera.
                var outstandingLoans = await _loansRepository.CountAsync(
                    loan => loan.Status == LoanStatus.Activo);

                var creditCardActive = await _creditCardsRepository.CountAsync(
                    card => card.Status == CreditCardStatus.Activa);

                //Incluye principales y secundarias
                var savingAccountActive = await _savingsAccountsRepository.CountAsync(
                    account => account.Status == SavingsAccountStatus.Activa);

                //Indicador 7: la suma de los tres anteriores
                var totalFinancialProducts = savingAccountActive + outstandingLoans + creditCardActive;

                //Indicador 11: deuda total de los clientes activos entre su cantidad. Se le pasan
                //los Ids de Identity para que el divisor sea la cantidad de clientes activos
                //reales, no solo la de los que tienen productos. Sin clientes activos: RD$0.00.
                var activeClientIds = activeClients.Select(client => client.Id).ToList();
                var averageDebt = await _debtCalculator.GetAverageDebtAsync(activeClientIds);

                // Volumen de los últimos 7 días
                var today = DateTimeOffset.UtcNow.Date;
                var startDate = today.AddDays(-6); // Hace 6 días + hoy = 7 días
                var last7DaysTxs = await _transactionRepository.GetTransactionsFromDateAsync(startDate);

                var labels = new List<string>();
                var txCounts = new List<int>();
                var pyCounts = new List<int>();

                // Asegurar que los 7 días existan aunque no tengan transacciones
                for (int i = 0; i < 7; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    // Etiqueta como "Lun 18"
                    var label = currentDate.ToString("ddd dd", new System.Globalization.CultureInfo("es-ES"));
                    // Capitalizar primera letra: "Lun 18" en lugar de "lun 18"
                    label = char.ToUpper(label[0]) + label.Substring(1);

                    var txsInDay = last7DaysTxs.Where(t => t.CreatedAt.Date == currentDate).ToList();
                    var txCount = txsInDay.Count;
                    var pyCount = txsInDay.Count(t => 
                        (t.OperationType == OperationType.PagoTarjeta || t.OperationType == OperationType.PagoPrestamo) 
                        && t.Status == TransactionStatus.Aprobada);

                    labels.Add(label);
                    txCounts.Add(txCount);
                    pyCounts.Add(pyCount);
                }

                var indicators = new AdminDashboardDto
                {
                    TotalHistoricalTransactions = totalHistoricalTransactions,
                    DayTransactions = dayTransactions,
                    TotalHistoricalPay = historicalPayments.Count,
                    DayPay = dayPayments.Count,
                    CustomerActive = customerActive,
                    CustomerInactive = customerInactive,
                    TotalFinancialProducts = totalFinancialProducts,
                    OutstandingLoans = outstandingLoans,
                    CreditCardActive = creditCardActive,
                    SavingAccountActive = savingAccountActive,
                    AverageDebtAmountPerCustomer = averageDebt,
                    Last7DaysLabels = labels,
                    Last7DaysTransactions = txCounts,
                    Last7DaysPayments = pyCounts
                };

                _logger.LogInformation(
                    "Indicadores del administrador: {Transacciones} transacciones ({TransaccionesDia} hoy), {Pagos} pagos ({PagosDia} hoy), {ClientesActivos} clientes activos, {Productos} productos financieros activos",
                    totalHistoricalTransactions, dayTransactions, historicalPayments.Count, dayPayments.Count,
                    customerActive, totalFinancialProducts);

                return ValidationResult<AdminDashboardDto>.Success(indicators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los indicadores generales del Home del administrador");
                return ValidationResult<AdminDashboardDto>.Failure(GeneralError.UnexpectedError);
            }
        }
    }
}
