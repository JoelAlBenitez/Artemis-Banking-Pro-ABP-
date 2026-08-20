using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Registration
{
    public class AccountRegistrationService : IAccountRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGenerateTokens _generateTokens;
        private readonly IEmailServices _emailServices;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<AccountRegistrationService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public AccountRegistrationService(
            UserManager<ApplicationUser> userManager,
            IGenerateTokens generateTokens,
            IEmailServices emailServices,
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository,
            ILogger<AccountRegistrationService> logger,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _generateTokens = generateTokens;
            _emailServices = emailServices;
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
        {
            _logger.LogInformation("Iniciando el registro del usuario con correo {Email}", request.Email);
            var response = new RegisterResponse();

            if (!Enum.TryParse<Roles>(request.Role, true, out var role))
            {
                response.HasError = true;
                response.Error = "El tipo de usuario seleccionado no es válido.";
                return response;
            }

            if (request.Password != request.ConfirmPassword)
            {
                _logger.LogWarning("Registro fallido: las contraseñas no coinciden para {Email}.", request.Email);
                response.HasError = true;
                response.Error = "La contraseña y la confirmación de contraseña deben coincidir.";
                return response;
            }

            //El monto inicial solo aplica a las cuentas de ahorro principales que se crean
            //automáticamente. Si no se indica, la cuenta abre en RD$0.00.
            var initialAmount = request.InitialAmount ?? 0m;
            if (initialAmount < 0)
            {
                response.HasError = true;
                response.Error = "El monto inicial no puede ser negativo.";
                return response;
            }

            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                _logger.LogWarning("Registro fallido: el correo {Email} ya está registrado.", request.Email);
                response.HasError = true;
                response.Conflict = true;
                response.Error = "Ya existe un usuario registrado con este correo electrónico.";
                return response;
            }

            if (await _userManager.FindByNameAsync(request.UserName) != null)
            {
                _logger.LogWarning("Registro fallido: el nombre de usuario {UserName} ya está registrado.", request.UserName);
                response.HasError = true;
                response.Conflict = true;
                response.Error = "Ya existe un usuario registrado con este nombre de usuario.";
                return response;
            }

            if (await _userManager.Users.AnyAsync(u => u.IDCARD == request.IDCARD))
            {
                _logger.LogWarning("Registro fallido: la cédula {IdCard} ya está registrada.", request.IDCARD);
                response.HasError = true;
                response.Conflict = true;
                response.Error = "Ya existe un usuario registrado con esta cédula.";
                return response;
            }

            //Todo usuario creado desde el sistema queda inactivo hasta completar la activación
            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                IDCARD = request.IDCARD,
                IsActive = false,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogError("Error al crear el usuario {UserName} en Identity: {Error}",
                    request.UserName, result.Errors.FirstOrDefault()?.Description);
                response.HasError = true;
                response.Error = result.Errors.FirstOrDefault()?.Description
                    ?? "No fue posible crear el usuario. Verifique los datos ingresados.";
                return response;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());
            if (!roleResult.Succeeded)
            {
                //Un usuario sin rol no puede operar en ningún módulo: se revierte la creación
                await _userManager.DeleteAsync(user);
                _logger.LogError("Error al asignar el rol {Role} al usuario {UserName}.", role, request.UserName);
                response.HasError = true;
                response.Error = "No fue posible asignar el tipo de usuario. Intente nuevamente.";
                return response;
            }

            response.UserId = user.Id;

            //Los clientes y los usuarios de comercio reciben automáticamente su cuenta de
            //ahorro principal. Administradores y cajeros no manejan productos financieros.
            if (role == Roles.Cliente || role == Roles.Comercio)
            {
                var accountCreated = await CreatePrimaryAccountAsync(user.Id, initialAmount);
                if (!accountCreated)
                {
                    await _userManager.DeleteAsync(user);
                    response.HasError = true;
                    response.Error = "No fue posible crear la cuenta de ahorro principal del cliente. Intente nuevamente.";
                    return response;
                }
            }

            if (!await SendActivationEmailAsync(user, request.Origin))
            {
                response.HasError = true;
                response.Error = "No fue posible enviar el correo de activación. Intente nuevamente más tarde.";
                return response;
            }

            _logger.LogInformation("Usuario {UserName} registrado correctamente con rol {Role}.", request.UserName, role);
            return response;
        }

        public async Task<ConfirmAccountResponse> ConfirmAccountAsync(string userId, string token)
        {
            _logger.LogInformation("Iniciando la confirmación de la cuenta del usuario {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return ConfirmError("El enlace de activación no es válido.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Confirmación fallida: el usuario {UserId} no existe.", userId);
                return ConfirmError("El enlace de activación no es válido.");
            }

            //El token de activación es de un solo uso: una cuenta ya confirmada lo rechaza
            if (user.EmailConfirmed)
            {
                _logger.LogWarning("Confirmación fallida: la cuenta del usuario {UserId} ya estaba activada.", userId);
                return ConfirmError("Este enlace de activación ya fue utilizado.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Confirmación fallida: token inválido o expirado para el usuario {UserId}.", userId);
                return ConfirmError("El enlace de activación no es válido.");
            }

            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Cuenta del usuario {UserId} activada correctamente.", userId);
            return new ConfirmAccountResponse
            {
                Message = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión."
            };
        }

        #region Helpers

        private static ConfirmAccountResponse ConfirmError(string message)
            => new() { HasError = true, Message = message };

        //Cuenta de ahorro principal: número único de 9 dígitos emitido por la secuencia del
        //módulo de cuentas, estado Activa y balance igual al monto inicial indicado.
        private async Task<bool> CreatePrimaryAccountAsync(string customerId, decimal initialAmount)
        {
            //Responsable de la apertura: el administrador que registra al cliente. En un
            //autorregistro no hay sesión, y ahí sí la apertura la origina el sistema.
            var performedByUserId = _currentUserService.UserId ?? DomainConstants.SystemUserId;

            try
            {
                var accountNumber = await _savingsAccountsRepository.GetNextAccountNumberAsync();
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    _logger.LogError("No fue posible generar el número de la cuenta principal del cliente {CustomerId}.", customerId);
                    return false;
                }

                var account = await _savingsAccountsRepository.AddAsync(new SavingsAccount
                {
                    AccountNumber = accountNumber,
                    CustomerId = customerId,
                    Balance = initialAmount,
                    AccountType = SavingsAccountType.Principal,
                    Status = SavingsAccountStatus.Activa,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = performedByUserId
                });
                await _savingsAccountsRepository.SaveChangesAsync();

                //Solo un monto inicial mayor que cero genera la transacción de apertura
                if (initialAmount > 0)
                {
                    await _transactionRepository.AddAsync(new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = initialAmount,
                        TransactionType = TransactionType.Credito,
                        OperationType = OperationType.AperturaCuenta,
                        Origin = "Apertura de cuenta",
                        Beneficiary = account.AccountNumber,
                        Status = TransactionStatus.Aprobada,
                        PerformedByUserId = performedByUserId,
                        Channel = ChannelPayment.Administrador,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreateByUserId = performedByUserId
                    });
                    await _transactionRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la cuenta de ahorro principal del cliente {CustomerId}.", customerId);
                return false;
            }
        }

        //La aplicación web envía un enlace de activación; la Web API envía el token en el
        //cuerpo del correo porque la activación se completa desde un endpoint.
        private async Task<bool> SendActivationEmailAsync(ApplicationUser user, string? origin)
        {
            try
            {
                string body;
                if (string.IsNullOrWhiteSpace(origin))
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    body = $"<p>Hola {user.FirstName},</p>" +
                           "<p>Su cuenta ha sido creada correctamente en Artemis Banking.</p>" +
                           "<p>Utilice el siguiente token para activar su cuenta desde el endpoint correspondiente:</p>" +
                           $"<p><strong>{token}</strong></p>" +
                           $"<p>Identificador de usuario: <strong>{user.Id}</strong></p>" +
                           "<p>Si usted no esperaba la creación de esta cuenta, ignore este mensaje.</p>";
                }
                else
                {
                    var verificationUri = await _generateTokens.GenerateTokenConfirmEmailAsync(user, origin);
                    body = $"<p>Hola {user.FirstName},</p>" +
                           "<p>Su cuenta ha sido creada correctamente en Artemis Banking.</p>" +
                           "<p>Para activar su usuario, haga clic en el siguiente enlace:</p>" +
                           $"<p><a href='{verificationUri}'>{verificationUri}</a></p>" +
                           "<p>Si usted no esperaba la creación de esta cuenta, ignore este mensaje.</p>";
                }

                return await _emailServices.SendNotification(new MessageDto
                {
                    To = user.Email!,
                    Subject = "Activación de cuenta",
                    Message = body
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo de activación al usuario {UserId}.", user.Id);
                return false;
            }
        }

        #endregion
    }
}
