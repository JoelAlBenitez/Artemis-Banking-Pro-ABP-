using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Management
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            ILogger<UserManagementService> logger,
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _logger = logger;
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
        }

        // 1
        public async Task<PagedResponseDto<UserDto>> GetUsersAsync(int page, int pageSize, StatusFilter status)
        {
            _logger.LogInformation("Obteniendo lista de usuarios. Pagina: {Page}, Status: {Status}", page, status);
            var query = _userManager.Users.AsNoTracking();

            if (status == StatusFilter.Activos)
                query = query.Where(u => u.IsActive);
            else if (status == StatusFilter.Inactivos)
                query = query.Where(u => !u.IsActive);

            var users = await query
                .OrderByDescending(u => u.IsActive)
                .ThenByDescending(u => u.CreatedAt)
                .ToListAsync();

            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Parte 3: El rol Comercio queda excluido de forma permanente
                if (roles.Contains(Roles.Comercio.ToString())) continue;

                var dto = _mapper.Map<UserDto>(user);
                if (System.Enum.TryParse<Roles>(roles.FirstOrDefault(), true, out var roleEnum))
                {
                    dto.TypeUser = roleEnum;
                }
                dtos.Add(dto);
            }

            var limit = pageSize > DomainConstants.MaxPageSize ? DomainConstants.MaxPageSize : pageSize;
            var pagedDtos = dtos.Skip((page - 1) * limit).Take(limit).ToList();

            return new PagedResponseDto<UserDto>
            {
                Items = pagedDtos,
                TotalCount = dtos.Count
            };
        }

        // 2
        public async Task<PagedResponseDto<UserDto>> GetUsersByRoleAsync(Roles role, int page, int pageSize)
        {
            _logger.LogInformation("Obteniendo usuarios por rol: {Role}. Pagina: {Page}", role, page);
            
            // Parte 3: El rol Comercio queda excluido de forma permanente
            if (role == Roles.Comercio) 
                return new PagedResponseDto<UserDto> { Items = new List<UserDto>(), TotalCount = 0 };

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.ToString());
            
            var dtos = usersInRole
                .OrderByDescending(u => u.IsActive)
                .ThenByDescending(u => u.CreatedAt)
                .Select(user => {
                    var dto = _mapper.Map<UserDto>(user);
                    dto.TypeUser = role;
                    return dto;
                })
                .ToList();

            var limit = pageSize > DomainConstants.MaxPageSize ? DomainConstants.MaxPageSize : pageSize;
            var pagedDtos = dtos.Skip((page - 1) * limit).Take(limit).ToList();

            return new PagedResponseDto<UserDto>
            {
                Items = pagedDtos,
                TotalCount = dtos.Count
            };
        }

        // 3
        public async Task<List<string>> GetRolesAsync()
        {
            // Parte 3: El rol Comercio queda excluido de forma permanente
            var roles = await _roleManager.Roles
                .Where(r => r.Name != Roles.Comercio.ToString())
                .Select(r => r.Name!)
                .ToListAsync();
            return roles;
        }

        // 5
        public async Task<bool> ToggleUserAsync(string userId, string currentUserId)
        {
            _logger.LogInformation("Iniciando activacion/desactivacion para usuario {UserId}", userId);
            if (userId == currentUserId)
            {
                _logger.LogWarning("Intento de desactivar propio usuario: {UserId}", userId);
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Parte 3: El rol Comercio queda excluido de forma permanente
                if (roles.Contains(Roles.Comercio.ToString())) return false;

                user.IsActive = !user.IsActive;
                user.EmailConfirmed = user.IsActive; 
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            return false;
        }

        // 6
        public async Task<UserDetailDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            // Parte 3: El rol Comercio queda excluido de forma permanente
            if (roles.Contains(Roles.Comercio.ToString())) return null;

            var dto = _mapper.Map<UserDetailDto>(user);
            dto.IsClient = roles.Contains(Roles.Cliente.ToString());
            return dto;
        }

        // 7
        public async Task<List<string>> GetRolesByUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            // Parte 3: El rol Comercio queda excluido de forma permanente
            return roles.Where(r => r != Roles.Comercio.ToString()).ToList();
        }

        // 8
        public async Task<ClientBaseDataDto?> GetClientBaseDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            // Parte 3: El rol Comercio queda excluido de forma permanente
            if (!roles.Contains(Roles.Cliente.ToString()) || roles.Contains(Roles.Comercio.ToString()))
                return null;

            return new ClientBaseDataDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }

        // 9
        public async Task<UserExistenceDto> ValidateUserExistsByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new UserExistenceDto { Exists = false, IsActive = false };

            var roles = await _userManager.GetRolesAsync(user);
            // Parte 3: El rol Comercio queda excluido de forma permanente
            if (roles.Contains(Roles.Comercio.ToString())) 
                return new UserExistenceDto { Exists = false, IsActive = false };

            return new UserExistenceDto { Exists = true, IsActive = user.IsActive };
        }

        public async Task<UserExistenceDto> ValidateUserExistsByIdCardAsync(string idCard)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.IDCARD == idCard);
            if (user == null) return new UserExistenceDto { Exists = false, IsActive = false };

            var roles = await _userManager.GetRolesAsync(user);
            // Parte 3: El rol Comercio queda excluido de forma permanente
            if (roles.Contains(Roles.Comercio.ToString())) 
                return new UserExistenceDto { Exists = false, IsActive = false };

            return new UserExistenceDto { Exists = true, IsActive = user.IsActive };
        }

        // 10
        public async Task<List<ClientSummaryDto>> GetActiveClientsAsync()
        {
            // Las consultas de cliente devuelven unicamente usuarios activos con rol Cliente.
            var usersInRole = await _userManager.GetUsersInRoleAsync(Roles.Cliente.ToString());
            
            return usersInRole
                .Where(u => u.IsActive)
                .Select(u => _mapper.Map<ClientSummaryDto>(u))
                .ToList();
        }

        // 11
        public async Task<IReadOnlyCollection<string>> GetActiveClientIdsAsync()
        {
            // Las consultas de cliente devuelven unicamente usuarios activos con rol Cliente.
            var usersInRole = await _userManager.GetUsersInRoleAsync(Roles.Cliente.ToString());
            return usersInRole.Where(u => u.IsActive).Select(u => u.Id).ToList().AsReadOnly();
        }

        // 12
        public async Task<ClientSummaryDto?> GetClientByIdCardAsync(string idCard)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.IDCARD == idCard);
            if (user == null || !user.IsActive) return null;

            var isClient = await _userManager.IsInRoleAsync(user, Roles.Cliente.ToString());
            if (!isClient) return null;

            return _mapper.Map<ClientSummaryDto>(user);
        }

        public async Task<RegisterResponse> UpdateUserAsync(string id, EditUserDto dto)
        {
            _logger.LogInformation("Actualizando datos del usuario con Id: {UserId}", id);
            var response = new RegisterResponse { HasError = false };

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "Usuario no encontrado.";
                return response;
            }

            // Validar que el correo electrónico no esté duplicado
            if (!string.Equals(user.Email, dto.Email, System.StringComparison.OrdinalIgnoreCase))
            {
                var userWithSameEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (userWithSameEmail != null)
                {
                    response.HasError = true;
                    response.Error = "Ya existe un usuario registrado con este correo electrónico.";
                    return response;
                }
            }

            // Validar que la cédula no esté duplicada
            if (user.IDCARD != dto.IDCARD)
            {
                var userWithSameIdCard = await _userManager.Users.FirstOrDefaultAsync(u => u.IDCARD == dto.IDCARD);
                if (userWithSameIdCard != null)
                {
                    response.HasError = true;
                    response.Error = "Ya existe un usuario registrado con esta cédula.";
                    return response;
                }
            }

            // Actualizar campos básicos
            user.FirstName = dto.Name;
            user.LastName = dto.LastName;
            user.IDCARD = dto.IDCARD;
            user.Email = dto.Email;
            user.UserName = dto.Email; // Opcional, dependiendo de si el username cambia con el email

            // Actualizar contraseña si se especificó una nueva
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (dto.NewPassword != dto.ConfirmNewPassword)
                {
                    response.HasError = true;
                    response.Error = "La contraseña y la confirmación de contraseña deben coincidir.";
                    return response;
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resultPassword = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!resultPassword.Succeeded)
                {
                    response.HasError = true;
                    response.Error = resultPassword.Errors.FirstOrDefault()?.Description ?? "Error al actualizar la contraseña.";
                    return response;
                }
            }

            // Guardar cambios del usuario
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = result.Errors.FirstOrDefault()?.Description ?? "Error al actualizar el usuario.";
                return response;
            }

            // Monto adicional para Clientes (si aplica)
            if (dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value > 0)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.Cliente.ToString()))
                {
                    // Buscar la cuenta principal activa del cliente
                    var primaryAccount = await _savingsAccountsRepository.GetFirstAsync(
                        a => a.CustomerId == user.Id && 
                             a.AccountType == SavingsAccountType.Principal && 
                             a.Status == SavingsAccountStatus.Activa
                    );

                    if (primaryAccount == null)
                    {
                        response.HasError = true;
                        response.Error = "No se encontró una cuenta principal activa para asignar el monto adicional.";
                        return response;
                    }

                    // Sumar el balance
                    primaryAccount.Balance += dto.AdditionalAmount.Value;
                    await _savingsAccountsRepository.UpdateAsync(primaryAccount);

                    // Registrar transacción de Crédito por Depósito (como ajuste administrativo)
                    var transaction = new Transaction
                    {
                        SavingsAccountId = primaryAccount.Id,
                        Amount = dto.AdditionalAmount.Value,
                        TransactionType = TransactionType.Credito,
                        OperationType = OperationType.Deposito, // Mantenemos deposito para no alterar enums ajenos
                        Origin = "Ajuste Administrativo",
                        Status = TransactionStatus.Aprobada,
                        PerformedByUserId = "SYSTEM",
                        Channel = ChannelPayment.Cajero,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreateByUserId = "SYSTEM"
                    };

                    await _transactionRepository.AddAsync(transaction);
                    await _transactionRepository.SaveChangesAsync();
                }
                else
                {
                    response.HasError = true;
                    response.Error = "El monto adicional solo se puede asignar a usuarios con rol Cliente.";
                    return response;
                }
            }

            return response;
        }
    }
}
