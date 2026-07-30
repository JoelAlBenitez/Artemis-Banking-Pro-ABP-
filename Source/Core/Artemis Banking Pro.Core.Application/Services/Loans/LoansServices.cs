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
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using AutoMapper;
using Microsoft.Extensions.Logging;
using static Artemis_Banking_Pro.Core.Application.Common.LogEvents;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
     public sealed class LoansServices :
        GenericServices<LoansAssignmentDto, LoansDto, int, Loan>,
        ILoansServices
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly IAmortizationCalculator _amortizationCalculator;
        private readonly IEmailServices _emailServices;
        private readonly ILoansValidateServices _loansValidateServices;
        private readonly ILogger<LoansServices> _logger;

        public LoansServices(
            ILoansRepository loansRepository,
            ILoanInstallmentRepository loanInstallmentRepository,
            IAmortizationCalculator amortizationCalculator,
            IEmailServices emailServices,
            IMapper mapper,
            ILoansValidateServices loansValidateServices,
            ILogger<LoansServices>  logger
           ) : base(loansRepository, mapper)

        {
            _loansRepository = loansRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            _amortizationCalculator = amortizationCalculator;
            _emailServices = emailServices;
            _loansValidateServices = loansValidateServices;
            _logger = logger;
        }


        #region query methods
        public async Task<ValidationResult<PagedResult<LoansDto>>> GetPagedLoansAsync(LoansFilterDto filter)
        {
            try
            {
                #region consulta de prestamos de un determinado cliente
                if (!string.IsNullOrWhiteSpace(filter.IdCard))
                {
                    _logger.LogInformation("Recuperando resultado de la validaciones de los filtros ingresados" +
                        " por el usuario para consulta del cliente con cédula {idCard}", filter.IdCard);
                    var resultV = await _loansValidateServices.GetLoansByCustomerValidateAsync(filter);
                    if (!resultV.IsValid) return (ValidationResult<PagedResult<LoansDto>>)resultV;

                    //agregar aqui obtencion del usuario de la cedula ingresada en el filtro | en espera de adrian

                    ////
                    ///
                    
                    _logger.LogInformation("Recuperando datos del cliente con Id {ID}", 0); //por cambiar
                    var result = await _loansRepository.GetPagedLoansAsync(
                        filter.Page,
                        DomainConstants.DefaultPageSize,
                         ToLoanStatus(filter.Status),
                        "");

                    //agregar mapeo extra para pasar al dto el full name del customer 
                    _logger.LogInformation("Mapeando entidad al dto con ID {ID}", 0); //por cambiar
                    var items = _mapper.Map<IReadOnlyCollection<LoansDto>>(result.Items);

                    _logger.LogInformation("Retornando prestamos del cliente consultado con paginación.");
                    var paged = new PagedResult<LoansDto>(
                        items, result.Page, result.PageSize, result.TotalRecords);
                    return ValidationResult<PagedResult<LoansDto>>.Success(paged);
                }
                #endregion
                #region consulta de todos los prestamos sin importar estado
                _logger.LogInformation("Recuperando todos los prestamos segun el estado seleccionado. Indepedencia del cliente indicado.");
                var resultAll = await _loansRepository.GetAllAsync(
                    filter.Page, DomainConstants.DefaultPageSize,
                    x => x.Status == ToLoanStatus(filter.Status), 
                    orderBy: x => x.OrderBy(q => q.Status),
                    x => x.loanInstallments
                    );
                //falta pasar el name full del customer
                _logger.LogInformation("Mapeando los datos recuperados");
                var itemsAll = _mapper.Map<IReadOnlyCollection<LoansDto>>(resultAll.Items);

                _logger.LogInformation("Retornanado el listado de prestamo según el estado {Status}", filter.Status.ToString());
                var pagedAll = new PagedResult<LoansDto>(
                        itemsAll, resultAll.Page, resultAll.PageSize, resultAll.TotalRecords);
                return ValidationResult<PagedResult<LoansDto>>.Success(pagedAll);
                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el listado de prestamos del cliente con ID {ID}", 0); //por cambiar
                return ValidationResult<PagedResult<LoansDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<DetailLoansDto>> GetDetailLoanAsync(int loanId)
        {
            try
            {
                _logger.LogInformation("Recuperando los detalles del prestamo con el ID {ID}", loanId);
                var loan = await _loansRepository.GetFirstAsync(x => x.Id == loanId);
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
                #region validations
                _logger.LogInformation("Recuperando el prestamo con ID {ID}", loanId);
                var loan = await _loansRepository.GetByIdAsync(loanId);

                _logger.LogInformation("Validando la existencia del prestamo con ID {ID}", loanId);
                var result = await _loansValidateServices.EditValidateAnnualInterestRateAsync(loanId);
                if (!result.IsValid) return (ValidationResult<EditAnnualInterestRateDto>)result;
                #endregion

                _logger.LogInformation("Mapeando entidad a dto con el ID {Id}", loanId);
                var dto = _mapper.Map<EditAnnualInterestRateDto>(loan);

                _logger.LogInformation("Retornando el dto del prestamo seleccionado con ID {ID}", loanId);
                return ValidationResult<EditAnnualInterestRateDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el prestamo con ID {ID}", loanId);
                return ValidationResult<EditAnnualInterestRateDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public override Task<ValidationResult<PagedResult<LoansDto>>> GetAllAsync(int page, int pageSize)
        {

            return base.GetAllAsync(page, pageSize);
        }
        
        #endregion

        #region write methos
        public async Task<ValidationResult> EditAnnualInterestRateAsync(EditAnnualInterestRateDto dto)
        {
            if (dto.AnnualInterestRate < 0m)
            {
                return ValidationResult.Failure(LoansError.NegativeAnnualInterestRate);
            }

            try
            {
                #region validacion de logica y obtencion de recalculo de cuotas
                _logger.LogInformation("Validando las reglas del negocio del prestamo con ID {ID}", dto.Id);
                var validate = await _loansValidateServices.EditValidateAnnualInterestRateAsync(dto.Id);
                if (!validate.IsValid) return validate;

                _logger.LogInformation("Recuperando los datos del prestamo con ID {ID}", dto.Id);
                var loan = await _loansRepository.GetFirstAsync(x => x.Id == dto.Id);
                var today = DateTimeOffset.UtcNow;
                var futureInstallments = loan!.loanInstallments
                    .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                    .ToList();

                //Solo se redistribuye el capital que aún no ha sido amortizado
                _logger.LogInformation("Recalculando las cuotas futuras pendientes del prestamo con ID {ID}", loan.Id);
                var pendingCapital = futureInstallments.Sum(i => i.CapitalAmount);
                var recalculated = _amortizationCalculator.RecalculateFutureInstallments(loan.loanInstallments.ToList(), pendingCapital, dto.AnnualInterestRate, today);
                #endregion

                #region mapeo de datos 
                _logger.LogInformation("Mapeo de datos del dto a la entidad de prestamos con ID {ID}", loan.Id);
                //cambiando campos especificos del prestamo
                loan.AnnualInterestRate = dto.AnnualInterestRate;
                loan.MonthlyInstallment = recalculated[0].InstallmentValue;
                loan.TotalPayable = loan.loanInstallments.Sum(i => i.InstallmentValue);
                loan.PendingAmount = loan.loanInstallments
                    .Where(i => i.paymentStatus != PaymentStatus.Pagada)
                    .Sum(i => i.PendingBalance);
                loan.ModifiedAt = today;

                //loan.LastModifiedByIdUser = adminUserId; // cambiar por el ICurrentUserServices | en espera por parte de adrian
                #endregion

               _logger.LogInformation("Intento de modificacion de los datos del prestamo y sus cuotas con ID {ID}", loan.Id);
               await _loansRepository.UpdateAsync(loan);
               await _loanInstallmentRepository.UpdateRangeLoansInstallmentAsync(recalculated.ToList());
               var result = await _loansRepository.SaveChangesAsync();
               if(result  < 0)
               {
                    _logger.LogError("Fallo al intentar modificar el prestamo y sus cuotas futuras con ID {ID}, registros modificados {r}", loan.Id, result);
                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

               //agregar aqui llamada al envio del correo electronico con la actualizacion de cuotas
               //cuando se integre el ICurrentUserServices para obtener email, full name....del cliente de este prestamo 

               return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar la tarifa del prestamo con ID {ID} ", dto.Id);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public override async Task<ValidationResult> CreateAsync(LoansAssignmentDto dto)
        {
            try
            {
                _logger.LogInformation("Verificando los datos de la asignación del prestamo al cliente con ID {ID}", dto.CustomerId);
                var result = await  _loansValidateServices.AssigmentLoansValidateAsync(dto);
                if (!result.IsValid) return result;

                //agregar aqui validaciones de alto riesgo crear metodo privado
                

                //agregar creacion de la entidad de loans, loans installment, envio de email pertinente 
                //return base.CreateAsync(dto);
                return ValidationResult.Success();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al crear el prestamo al cliente con ID {ID}", dto.CustomerId);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }

           
        }
        #endregion


        //modificar el metodo de envio de actualizacion de cuotas por tarifa

        //agregar el envio de correo electronico de una asignacion de prestamo
        #region private methods
        private static string BuildRateUpdateBody(LoanRateUpdatedDto rateUpdated, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               $"<p>La tasa de interés de su préstamo {rateUpdated.LoanNumber} ha sido actualizada.</p>" +
               $"<p>Nueva tasa de interés anual: {rateUpdated.AnnualInterestRate}%<br/>" +
               $"Nuevo valor de la próxima cuota: RD${rateUpdated.NextInstallmentValue:N2}<br/>" +
               $"Fecha de vencimiento de la próxima cuota: {rateUpdated.NextInstallmentDueDate:dd/MM/yyyy}</p>" +
               "<p>Esta modificación aplica únicamente a las cuotas futuras pendientes.</p>";

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

            return sent
                ? ValidationResult.Success()
                : ValidationResult.Failure(LoansError.RateUpdatedWithoutNotification);
        }

        private static LoanStatus ToLoanStatus(LoanStatusFilter filter)
            => filter switch
            {
                LoanStatusFilter.Activos => LoanStatus.Activo,
                LoanStatusFilter.Todos => LoanStatus.Activo,
                _ => LoanStatus.Completado
                };
        #endregion

    }
}
