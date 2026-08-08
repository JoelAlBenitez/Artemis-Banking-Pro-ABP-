using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
    public sealed class LoansServices :
       GenericServices<LoansAssignmentDto, LoansDto, int, Loan>,
       ILoansServices
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAmortizationCalculator _amortizationCalculator;
        private readonly IDebtCalculator _debtCalculator;
        private readonly IEmailServices _emailServices;
        private readonly ILoansValidateServices _loansValidateServices;
        private readonly ILogger<LoansServices> _logger;

        public LoansServices(
            ILoansRepository loansRepository,
            ILoanInstallmentRepository loanInstallmentRepository,
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository,
            IAmortizationCalculator amortizationCalculator,
            IDebtCalculator debtCalculator,
            IEmailServices emailServices,
            IMapper mapper,
            ILoansValidateServices loansValidateServices,
            ILogger<LoansServices> logger
           ) : base(loansRepository, mapper, logger)
        {
            _loansRepository = loansRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
            _amortizationCalculator = amortizationCalculator;
            _debtCalculator = debtCalculator;
            _emailServices = emailServices;
            _loansValidateServices = loansValidateServices;
            _logger = logger;
        }

        #region query methods
        public async Task<ValidationResult<PagedResult<LoansDto>>> GetPagedLoansAsync(LoansFilterDto filter)
        {
            try
            {
                var validation = await _loansValidateServices.GetLoansByCustomerValidateAsync(filter);
                if (!validation.IsValid)
                {
                    return ValidationResult<PagedResult<LoansDto>>.Failure(validation.Errors.ToList());
                }

                //La cédula identifica al cliente dentro del project Identity. Cuando su servicio de
                //consulta de usuarios esté disponible, aquí se traduce a su ID y se pasa al filtro.
                //customerId = (await _userServices.GetCustomerByIdCardAsync(filter.IdCard))?.Id;
                string? customerId = null;

                _logger.LogInformation("Recuperando el listado de prestamos. Pagina {Pagina}, estado {Estado}",
                    filter.Page, filter.Status);

                var result = await _loansRepository.GetPagedLoansAsync(
                    filter.Page,
                    filter.PageSize,
                    ToLoanStatus(filter.Status),
                    customerId);

                if (!string.IsNullOrWhiteSpace(filter.IdCard) && result.TotalRecords == 0)
                {
                    _logger.LogWarning("El cliente consultado no tiene prestamos registrados");

                    return ValidationResult<PagedResult<LoansDto>>.Failure(
                        filter.Status == LoanStatusFilter.Todos
                            ? LoansError.NonExistsLoans
                            : LoansError.NonExistLoansByIndicateState);
                }

                //El nombre del cliente de cada préstamo proviene del project Identity y se completa
                //cuando su servicio de consulta de usuarios esté disponible.

                var items = _mapper.Map<IReadOnlyCollection<LoansDto>>(result.Items);
                var paged = new PagedResult<LoansDto>(items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<LoansDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el listado de prestamos");
                return ValidationResult<PagedResult<LoansDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<DetailLoansDto>> GetDetailLoanAsync(int loanId)
        {
            try
            {
                _logger.LogInformation("Recuperando los detalles del prestamo con el ID {ID}", loanId);

                //Sin las cuotas la tabla de amortización del detalle saldría vacía
                var loan = await _loansRepository.GetFirstAsync(x => x.Id == loanId, x => x.loanInstallments);

                if (loan is null)
                {
                    _logger.LogWarning("Detalles del prestamo con ID {ID} no fueron encontrados", loanId);
                    return ValidationResult<DetailLoansDto>.Failure(LoansError.NonExistsLoan);
                }

                _logger.LogInformation("Retornando los detalles del prestamo con ID {ID}", loanId);
                return ValidationResult<DetailLoansDto>.Success(_mapper.Map<DetailLoansDto>(loan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles del prestamo con ID {ID}", loanId);
                return ValidationResult<DetailLoansDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<EditAnnualInterestRateDto>> GetLoanForEditRateAsync(int loanId)
        {
            try
            {
                _logger.LogInformation("Validando la existencia del prestamo con ID {ID}", loanId);
                var result = await _loansValidateServices.EditValidateAnnualInterestRateAsync(loanId);
                if (!result.IsValid)
                {
                    return ValidationResult<EditAnnualInterestRateDto>.Failure(result.Errors.ToList());
                }

                _logger.LogInformation("Retornando el dto del prestamo seleccionado con ID {ID}", loanId);
                return ValidationResult<EditAnnualInterestRateDto>.Success(
                    _mapper.Map<EditAnnualInterestRateDto>(result.Value!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el prestamo con ID {ID}", loanId);
                return ValidationResult<EditAnnualInterestRateDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<LoansDto>>> GetActiveLoansByCustomerAsync(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return ValidationResult<IReadOnlyCollection<LoansDto>>.Failure(LoansError.NonSelectedCustomer);
                }

                _logger.LogInformation("Recuperando los prestamos activos del cliente con ID {ID}", customerId);

                var loans = await _loansRepository.GetAllFindAsync(
                    loan => loan.CustomerId == customerId && loan.Status == LoanStatus.Activo,
                    loan => loan.loanInstallments);

                return ValidationResult<IReadOnlyCollection<LoansDto>>.Success(
                    _mapper.Map<IReadOnlyCollection<LoansDto>>(loans));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los prestamos activos del cliente con ID {ID}", customerId);
                return ValidationResult<IReadOnlyCollection<LoansDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<ClientsForLoanAssignmentDto>> GetCustomersForAssignmentAsync(string? idCard)
        {
            try
            {
                _logger.LogInformation("Recuperando los clientes elegibles para la asignacion de un prestamo");

                //El monto promedio de deuda sí pertenece a este módulo y se muestra arriba del listado
                var averageDebt = await _debtCalculator.GetAverageDebtAsync();

                //El listado de clientes activos, su nombre, su correo y la búsqueda por cédula
                //provienen del project Identity. Cuando su servicio de consulta de usuarios esté
                //disponible, este bloque se descomenta: se descartan los clientes con préstamo
                //activo y se completa la deuda de cada uno con el IDebtCalculator.
                //var activeCustomers = string.IsNullOrWhiteSpace(idCard)
                //    ? await _userServices.GetActiveCustomersAsync()
                //    : await _userServices.GetCustomerByIdCardAsync(idCard);
                //var customerIds = activeCustomers.Select(customer => customer.Id).ToList();
                //var withActiveLoan = await _loansRepository.GetAllFindAsync(
                //    loan => loan.Status == LoanStatus.Activo && customerIds.Contains(loan.CustomerId));
                //var eligible = activeCustomers
                //    .Where(customer => withActiveLoan.All(loan => loan.CustomerId != customer.Id))
                //    .ToList();
                //var debts = await _debtCalculator.GetCustomersDebtAsync(
                //    eligible.Select(customer => customer.Id).ToList());
                //var clients = eligible.Select(customer => new ClientLoansDto
                //{
                //    Id = customer.Id,
                //    IdCard = customer.IdCard,
                //    FullName = customer.FullName,
                //    Email = customer.Email,
                //    TotalDebtAmount = debts[customer.Id]
                //}).ToList();

                var result = new ClientsForLoanAssignmentDto
                {
                    AverageDebt = averageDebt,
                    Clients = new List<ClientLoansDto>()
                };

                return ValidationResult<ClientsForLoanAssignmentDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los clientes elegibles para la asignacion de un prestamo");
                return ValidationResult<ClientsForLoanAssignmentDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public override async Task<ValidationResult<PagedResult<LoansDto>>> GetAllAsync(int page, int pageSize)
            => await GetPagedLoansAsync(new LoansFilterDto
            {
                Status = LoanStatusFilter.Todos,
                Page = page,
                PageSize = pageSize
            });
        #endregion

        #region write methods
        public async Task<ValidationResult<LoanRiskEvaluationDto>> EvaluateRiskAsync(LoansAssignmentDto dto)
        {
            try
            {
                if (dto is null || string.IsNullOrWhiteSpace(dto.CustomerId))
                {
                    return ValidationResult<LoanRiskEvaluationDto>.Failure(LoansError.NonSelectedCustomer);
                }

                _logger.LogInformation("Evaluando el riesgo del cliente con ID {ID} antes de registrar el prestamo",
                    dto.CustomerId);

                var averageDebt = await _debtCalculator.GetAverageDebtAsync();
                var currentDebt = await _debtCalculator.GetCustomerDebtAsync(dto.CustomerId);

                //El total a pagar sale de la suma de las cuotas de la tabla de amortización: se
                //calcula en memoria, sin persistir nada, porque el préstamo aún no existe.
                var totalPayable = BuildAmortizationTable(dto, DateTimeOffset.UtcNow)
                    .Sum(installment => installment.InstallmentValue);

                var projectedDebt = _debtCalculator.GetProjectedDebt(currentDebt, totalPayable);

                var evaluation = new LoanRiskEvaluationDto
                {
                    RiskType = LoanRiskType.SinRiesgo,
                    Message = string.Empty,
                    CurrentDebt = currentDebt,
                    ProjectedDebt = projectedDebt,
                    AverageDebt = averageDebt
                };

                if (currentDebt > averageDebt)
                {
                    evaluation.RiskType = LoanRiskType.DeudaActual;
                    evaluation.Message = LoansError.CustomerWithCurrentHighRisk.Description;
                }
                else if (projectedDebt > averageDebt)
                {
                    evaluation.RiskType = LoanRiskType.DeudaProyectada;
                    evaluation.Message = LoansError.CustomerWithProjectedHighRisk.Description;
                }

                _logger.LogInformation(
                    "Riesgo del cliente con ID {ID}: {Riesgo}. Deuda actual RD${Actual}, proyectada RD${Proyectada}, promedio RD${Promedio}",
                    dto.CustomerId, evaluation.RiskType, currentDebt, projectedDebt, averageDebt);

                return ValidationResult<LoanRiskEvaluationDto>.Success(evaluation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar el riesgo del cliente con ID {ID}", dto?.CustomerId);
                return ValidationResult<LoanRiskEvaluationDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public override async Task<ValidationResult> CreateAsync(LoansAssignmentDto dto)
        {
            _logger.LogInformation("Inicio de la asignacion de un prestamo al cliente con ID {ID}", dto.CustomerId);

            //Las validaciones de negocio van antes de tocar la base de datos
            var validation = await _loansValidateServices.AssigmentLoansValidateAsync(dto);
            if (!validation.IsValid) return validation;

            var risk = await EvaluateRiskAsync(dto);
            if (!risk.IsValid) return ValidationResult.Failure(risk.Errors.ToList());

            //Sin confirmación del administrador el préstamo no se crea: la WebApp muestra la
            //pantalla de advertencia y la Web API responde 409 Conflict con el mismo mensaje.
            if (risk.Value!.RequiresConfirmation && !dto.ConfirmHighRisk)
            {
                _logger.LogWarning("Asignacion detenida: el cliente con ID {ID} es de alto riesgo ({Riesgo})",
                    dto.CustomerId, risk.Value.RiskType);

                return ValidationResult.Failure(risk.Value.RiskType == LoanRiskType.DeudaActual
                    ? LoansError.CustomerWithCurrentHighRisk
                    : LoansError.CustomerWithProjectedHighRisk);
            }

            try
            {
                //Número de 9 dígitos emitido por LoanNumberSequence. Su rango es disjunto del de
                //las cuentas de ahorro, así que la unicidad entre ambos productos está garantizada
                //por la base de datos sin consultar registros.
                var loanNumber = await _loansRepository.GetNextLoanNumberAsync();
                if (string.IsNullOrWhiteSpace(loanNumber))
                {
                    _logger.LogError("No fue posible generar un numero de prestamo para el cliente con ID {ID}",
                        dto.CustomerId);

                    return ValidationResult.Failure(LoansError.FailedGenerateLoanNumber);
                }

                var assignedAt = DateTimeOffset.UtcNow;

                //Receptora del desembolso. La validación ya garantizó que existe y está activa.
                var primaryAccount = await _savingsAccountsRepository.GetFirstAsync(
                    account => account.CustomerId == dto.CustomerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (primaryAccount is null)
                {
                    _logger.LogWarning("Asignacion abortada: el cliente con ID {ID} dejo de tener una cuenta principal activa",
                        dto.CustomerId);

                    return ValidationResult.Failure(LoansError.NonExistAccountFirstActive);
                }

                var installments = BuildAmortizationTable(dto, assignedAt);
                if (installments.Count == 0)
                {
                    _logger.LogError("No fue posible generar la tabla de amortizacion del prestamo {LoanNumber}", loanNumber);
                    return ValidationResult.Failure(LoansError.FaildGenerateLoansInstallment);
                }

                var loan = _mapper.Map<Loan>(dto);
                loan.LoanNumber = loanNumber;
                loan.Status = LoanStatus.Activo;
                loan.MonthlyInstallment = installments[0].InstallmentValue;
                loan.TotalPayable = installments.Sum(installment => installment.InstallmentValue);
                loan.PendingAmount = loan.TotalPayable;
                loan.CreatedAt = assignedAt;
                loan.CreateByUserId = ""; // administrador responsable, por modificar

                //Las cuotas viajan en la navegación: EF les asigna el LoanId al insertar el préstamo
                foreach (var installment in installments) loan.loanInstallments.Add(installment);

                await _loansRepository.AddAsync(loan);

                //Desembolso: el capital aprobado se suma al balance de la cuenta principal y queda
                //registrado como una transacción de tipo CRÉDITO cuyo origen es el préstamo.
                primaryAccount.Balance += dto.AmmountLoans;
                primaryAccount.ModifiedAt = assignedAt;
                primaryAccount.LastModifiedByIdUser = ""; // por modificar

                await _savingsAccountsRepository.UpdateAsync(primaryAccount);

                await _transactionRepository.AddAsync(new Transaction
                {
                    SavingsAccountId = primaryAccount.Id,
                    Amount = dto.AmmountLoans,
                    TransactionType = TransactionType.Credito,
                    OperationType = OperationType.DesembolsoPrestamo,
                    Origin = loanNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    PerformedByUserId = "", // administrador responsable, por modificar
                    Channel = ChannelPayment.Administrador,
                    CreatedAt = assignedAt,
                    CreateByUserId = "" // por modificar
                });

                //Un único SaveChangesAsync confirma el préstamo, sus n cuotas, el nuevo balance de
                //la principal y el asiento de CRÉDITO: la operación es atómica.
                var result = await _loansRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("El prestamo {LoanNumber} del cliente con ID {ID} fallo en su asignacion",
                        loanNumber, dto.CustomerId);

                    return ValidationResult.Failure(LoansError.FailedProcessLoan);
                }

                _logger.LogInformation(
                    "Prestamo {LoanNumber} asignado al cliente con ID {ID}. Capital RD${Capital} desembolsado en la cuenta {Cuenta}",
                    loanNumber, dto.CustomerId, dto.AmmountLoans, primaryAccount.AccountNumber);

                //El correo de aprobación va fuera de la transacción: un fallo de correo no revierte
                //el préstamo. El destinatario y el nombre del cliente provienen del project Identity
                //y esta llamada se descomenta cuando su servicio los exponga.
                //var assigned = new LoanAssignedDto
                //{
                //    CustomerId = dto.CustomerId,
                //    LoanNumber = loanNumber,
                //    ApprovedAmount = dto.AmmountLoans,
                //    Term = (int)dto.TermLoans,
                //    AnnualInterestRate = dto.AnnualInterestRate,
                //    MonthlyInstallment = loan.MonthlyInstallment
                //};
                //return await SendLoanApprovedNotificationAsync(assigned, customerEmail, customerFullName);

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el prestamo al cliente con ID {ID}", dto.CustomerId);
                return ValidationResult.Failure(LoansError.FailedProcessLoan);
            }
        }

        public async Task<ValidationResult> EditAnnualInterestRateAsync(EditAnnualInterestRateDto dto)
        {
            if (dto.AnnualInterestRate < 0m)
            {
                return ValidationResult.Failure(LoansError.NegativeAnnualInterestRate);
            }

            _logger.LogInformation("Validando las reglas del negocio del prestamo con ID {ID}", dto.Id);
            var validation = await _loansValidateServices.EditValidateAnnualInterestRateAsync(dto.Id);
            if (!validation.IsValid) return ValidationResult.Failure(validation.Errors.ToList());

            try
            {
                var loan = validation.Value!;
                var today = DateTimeOffset.UtcNow;

                //Solo se redistribuye el capital que aún no ha sido amortizado
                var pendingCapital = loan.loanInstallments
                    .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                    .Sum(i => i.CapitalAmount);

                _logger.LogInformation("Recalculando las cuotas futuras pendientes del prestamo con ID {ID}", loan.Id);
                var recalculated = _amortizationCalculator.RecalculateFutureInstallments(
                    loan.loanInstallments.ToList(), pendingCapital, dto.AnnualInterestRate, today);

                loan.AnnualInterestRate = dto.AnnualInterestRate;
                loan.MonthlyInstallment = recalculated[0].InstallmentValue;
                loan.TotalPayable = loan.loanInstallments.Sum(i => i.InstallmentValue);
                loan.PendingAmount = loan.loanInstallments
                    .Where(i => i.paymentStatus != PaymentStatus.Pagada)
                    .Sum(i => i.PendingBalance);
                loan.ModifiedAt = today;
                loan.LastModifiedByIdUser = ""; // por modificar

                _logger.LogInformation("Intento de modificacion del prestamo y sus cuotas con ID {ID}", loan.Id);
                await _loansRepository.UpdateAsync(loan);
                await _loanInstallmentRepository.UpdateRangeLoansInstallmentAsync(recalculated.ToList());

                var result = await _loansRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogError("Fallo al modificar el prestamo y sus cuotas futuras con ID {ID}, registros modificados {r}",
                        loan.Id, result);

                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                //El correo de actualización de tasa va fuera de la transacción. El destinatario y el
                //nombre del cliente provienen del project Identity y esta llamada se descomenta
                //cuando su servicio los exponga.
                //var nextInstallment = recalculated.OrderBy(i => i.InstallmentNumber).First();
                //var rateUpdated = new LoanRateUpdatedDto
                //{
                //    CustomerId = loan.CustomerId,
                //    LoanNumber = loan.LoanNumber,
                //    AnnualInterestRate = loan.AnnualInterestRate,
                //    NextInstallmentValue = nextInstallment.InstallmentValue,
                //    NextInstallmentDueDate = nextInstallment.DueDate
                //};
                //return await SendRateUpdateNotificationAsync(rateUpdated, customerEmail, customerFullName);

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar la tasa del prestamo con ID {ID} ", dto.Id);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }
        #endregion

        #region private methods
        private List<LoanInstallment> BuildAmortizationTable(LoansAssignmentDto dto, DateTimeOffset createdAt)
            => _amortizationCalculator.GenerateAmortizationTable(
                dto.AmmountLoans,
                dto.AnnualInterestRate,
                (int)dto.TermLoans,
                createdAt,
                0).ToList();

        private static string BuildRateUpdateBody(LoanRateUpdatedDto rateUpdated, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               $"<p>La tasa de interés de su préstamo {rateUpdated.LoanNumber} ha sido actualizada.</p>" +
               $"<p>Nueva tasa de interés anual: {rateUpdated.AnnualInterestRate}%<br/>" +
               $"Nuevo valor de la próxima cuota: RD${rateUpdated.NextInstallmentValue:N2}<br/>" +
               $"Fecha de vencimiento de la próxima cuota: {rateUpdated.NextInstallmentDueDate:dd/MM/yyyy}</p>" +
               "<p>Esta modificación aplica únicamente a las cuotas futuras pendientes.</p>";

        private static string BuildLoanApprovedBody(LoanAssignedDto assigned, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               "<p>Su préstamo ha sido aprobado correctamente.</p>" +
               $"<p>Número de préstamo: {assigned.LoanNumber}<br/>" +
               $"Monto aprobado: RD${assigned.ApprovedAmount:N2}<br/>" +
               $"Plazo: {assigned.Term} meses<br/>" +
               $"Tasa de interés anual: {assigned.AnnualInterestRate}%<br/>" +
               $"Cuota mensual: RD${assigned.MonthlyInstallment:N2}</p>" +
               "<p>El monto aprobado ha sido depositado en su cuenta de ahorro principal.</p>";

        private async Task<ValidationResult> SendLoanApprovedNotificationAsync(
            LoanAssignedDto assigned, string customerEmail, string customerFullName)
        {
            var message = new MessageDto
            {
                To = customerEmail,
                Subject = "Préstamo aprobado",
                Message = BuildLoanApprovedBody(assigned, customerFullName)
            };

            var sent = await _emailServices.SendNotification(message);

            if (!sent)
            {
                _logger.LogWarning("No fue posible enviar el correo de aprobacion del prestamo {LoanNumber}. La operacion no se revierte",
                    assigned.LoanNumber);

                return ValidationResult.Failure(LoansError.LoanCreatedWithoutNotification);
            }

            return ValidationResult.Success();
        }

        private async Task<ValidationResult> SendRateUpdateNotificationAsync(
            LoanRateUpdatedDto rateUpdated, string customerEmail, string customerFullName)
        {
            var message = new MessageDto
            {
                To = customerEmail,
                Subject = "Actualización de tasa de interés de préstamo",
                Message = BuildRateUpdateBody(rateUpdated, customerFullName)
            };

            var sent = await _emailServices.SendNotification(message);

            if (!sent)
            {
                _logger.LogWarning("No fue posible enviar el correo de actualizacion de tasa del prestamo {LoanNumber}. La operacion no se revierte",
                    rateUpdated.LoanNumber);

                return ValidationResult.Failure(LoansError.RateUpdatedWithoutNotification);
            }

            return ValidationResult.Success();
        }

        private static LoanStatus? ToLoanStatus(LoanStatusFilter filter)
            => filter switch
            {
                LoanStatusFilter.Activos => LoanStatus.Activo,
                LoanStatusFilter.Completados => LoanStatus.Completado,
                _ => null
            };
        #endregion

    }
}