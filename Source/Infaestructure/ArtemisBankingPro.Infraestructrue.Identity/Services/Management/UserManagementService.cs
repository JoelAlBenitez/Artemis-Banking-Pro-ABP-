using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly ICurrentUserService _currentUserService;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            ILogger<UserManagementService> logger,
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _logger = logger;
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
            _currentUserService = currentUserService;
        }

        // 1
        public async Task<PagedResponseDto<UserDto>> GetUsersAsync(int page, int pageSize, StatusFilter status)
        {
            _logger.LogInformation("Obteniendo listado de usuarios. Página: {Page}, Estado: {Status}", page, status);

            var query = _userManager.Users.AsNoTracking();

            if (status == StatusFilter.Activos)
                query = query.Where(u => u.IsActive);
            else if (status == StatusFilter.Inactivos)
                query = query.Where(u => !u.IsActive);

            //Orden exigido por el contrato: del usuario más reciente al más antiguo
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                //El rol Comercio queda excluido de forma permanente del mantenimiento de usuarios
                if (roles.Contains(Roles.Comercio.ToString())) continue;

                dtos.Add(BuildUserDto(user, roles));
            }

            return Paginate(dtos, page, pageSize);
        }

        // 2
        public async Task<PagedResponseDto<UserDto>> GetUsersByRoleAsync(Roles role, int page, int pageSize)
        {
            _logger.LogInformation("Obteniendo usuarios por rol: {Role}. Página: {Page}", role, page);

            //El rol Comercio queda excluido de forma permanente
            if (role == Roles.Comercio)
                return Paginate(new List<UserDto>(), page, pageSize);

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.ToString());

            var dtos = usersInRole
                .OrderByDescending(u => u.CreatedAt)
                .Select(user =>
                {
                    var dto = _mapper.Map<UserDto>(user);
                    dto.TypeUser = role;
                    return dto;
                })
                .ToList();

            return Paginate(dtos, page, pageSize);
        }

        // 3
        public async Task<List<string>> GetRolesAsync()
        {
            //El rol Comercio queda excluido de forma permanente
            return await _roleManager.Roles
                .Where(r => r.Name != null && r.Name != Roles.Comercio.ToString())
                .Select(r => r.Name!)
                .ToListAsync();
        }

        // 5
        public async Task<UserOperationResponseDto> ToggleUserAsync(string userId)
        {
            var user = await FindUserAsync(userId);
            if (user == null)
                return NotFoundResponse();

            return await ApplyStatusAsync(user, !user.IsActive);
        }

        public async Task<UserOperationResponseDto> SetUserStatusAsync(string userId, bool status)
        {
            var user = await FindUserAsync(userId);
            if (user == null)
                return NotFoundResponse();

            return await ApplyStatusAsync(user, status);
        }

        // 6
        public async Task<UserDetailDto?> GetUserByIdAsync(string userId)
        {
            var user = await FindUserAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            //El rol Comercio queda excluido de forma permanente
            if (roles.Contains(Roles.Comercio.ToString())) return null;

            var dto = _mapper.Map<UserDetailDto>(user);
            dto.IsClient = roles.Contains(Roles.Cliente.ToString());
            if (Enum.TryParse<Roles>(roles.FirstOrDefault(), true, out var roleEnum))
                dto.TypeUser = roleEnum;

            return dto;
        }

        //Carga de la pantalla de edición: las mismas reglas que rechazan el guardado deciden
        //también si la pantalla llega a pintarse.
        public async Task<UserEditResponseDto> GetUserForEditAsync(string userId)
        {
            if (IsCurrentUser(userId))
            {
                _logger.LogWarning("Intento de editar la propia cuenta: {UserId}", userId);
                return new UserEditResponseDto
                {
                    HasError = true,
                    Error = "No puede editar su propia cuenta desde este módulo."
                };
            }

            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return new UserEditResponseDto
                {
                    HasError = true,
                    NotFound = true,
                    Error = "El usuario seleccionado no existe."
                };

            return new UserEditResponseDto { User = user };
        }

        // 7
        public async Task<List<string>> GetRolesByUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            //El rol Comercio queda excluido de forma permanente
            return roles.Where(r => r != Roles.Comercio.ToString()).ToList();
        }

        // 8
        public async Task<ClientBaseDataDto?> GetClientBaseDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Roles.Cliente.ToString())) return null;

            return _mapper.Map<ClientBaseDataDto>(user);
        }

        //Titular en una sola cadena para el módulo Cajero. No exige rol Cliente: el cajero
        //también muestra el titular de productos consultados por número de cuenta.
        public async Task<string?> GetFullNameByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return $"{user.FirstName} {user.LastName}".Trim();
        }

        // 9
        public async Task<UserExistenceDto> ValidateUserExistsByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return await BuildExistenceAsync(user);
        }

        public async Task<UserExistenceDto> ValidateUserExistsByIdCardAsync(string idCard)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.IDCARD == idCard);
            return await BuildExistenceAsync(user);
        }

        // 10
        public async Task<List<ClientSummaryDto>> GetActiveClientsAsync()
        {
            //Las consultas de cliente devuelven únicamente usuarios activos con rol Cliente
            var usersInRole = await _userManager.GetUsersInRoleAsync(Roles.Cliente.ToString());

            return usersInRole
                .Where(u => u.IsActive)
                .Select(u => _mapper.Map<ClientSummaryDto>(u))
                .ToList();
        }

        // 11
        public async Task<IReadOnlyCollection<string>> GetActiveClientIdsAsync()
        {
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

        public async Task<UserOperationResponseDto> UpdateUserAsync(string id, EditUserDto dto)
        {
            _logger.LogInformation("Actualizando los datos del usuario con Id: {UserId}", id);
            var response = new UserOperationResponseDto();

            //El administrador autenticado no puede editar su propia cuenta desde este módulo
            if (IsCurrentUser(id))
            {
                _logger.LogWarning("Intento de editar la propia cuenta: {UserId}", id);
                return Failure("No puede editar su propia cuenta desde este módulo.");
            }

            var user = await FindUserAsync(id);
            if (user == null)
                return NotFoundResponse();

            //El monto adicional se valida antes de tocar el usuario: si es inválido no se
            //modifica ningún dato
            if (dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value < 0)
                return Failure("El monto adicional no puede ser negativo.");

            if (!string.IsNullOrWhiteSpace(dto.NewPassword) && string.IsNullOrWhiteSpace(dto.ConfirmNewPassword))
                return Failure("Debe confirmar la nueva contraseña.");

            if (!string.IsNullOrWhiteSpace(dto.NewPassword) && dto.NewPassword != dto.ConfirmNewPassword)
                return Failure("La contraseña y la confirmación de contraseña deben coincidir.");

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var userWithSameEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (userWithSameEmail != null && userWithSameEmail.Id != user.Id)
                    return Conflict("Ya existe otro usuario registrado con este correo electrónico.");
            }

            if (!string.Equals(user.UserName, dto.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var userWithSameUserName = await _userManager.FindByNameAsync(dto.UserName);
                if (userWithSameUserName != null && userWithSameUserName.Id != user.Id)
                    return Conflict("Ya existe otro usuario registrado con este nombre de usuario.");
            }

            if (user.IDCARD != dto.IDCARD)
            {
                var userWithSameIdCard = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.IDCARD == dto.IDCARD && u.Id != user.Id);
                if (userWithSameIdCard != null)
                    return Conflict("Ya existe otro usuario registrado con esta cédula.");
            }

            user.FirstName = dto.Name;
            user.LastName = dto.LastName;
            user.IDCARD = dto.IDCARD;
            user.Email = dto.Email;
            user.UserName = dto.UserName;

            //Si el campo contraseña se deja vacío, la contraseña actual no se modifica
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resultPassword = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!resultPassword.Succeeded)
                    return Failure(resultPassword.Errors.FirstOrDefault()?.Description
                        ?? "No fue posible actualizar la contraseña del usuario.");
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Failure(result.Errors.FirstOrDefault()?.Description
                    ?? "No fue posible actualizar el usuario.");

            //Monto adicional: solo aplica a clientes y se suma a su cuenta de ahorro principal
            if (dto.AdditionalAmount.HasValue && dto.AdditionalAmount.Value > 0)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(Roles.Cliente.ToString()))
                    return Failure("El monto adicional solo puede asignarse a usuarios con rol Cliente.");

                var primaryAccount = await _savingsAccountsRepository.GetFirstAsync(
                    a => a.CustomerId == user.Id &&
                         a.AccountType == SavingsAccountType.Principal &&
                         a.Status == SavingsAccountStatus.Activa);

                if (primaryAccount == null)
                    return Failure("No se encontró una cuenta de ahorro principal activa para asignar el monto adicional.");

                primaryAccount.Balance += dto.AdditionalAmount.Value;
                await _savingsAccountsRepository.UpdateAsync(primaryAccount);

                //El aumento de balance queda registrado como CRÉDITO originado por el administrador
                await _transactionRepository.AddAsync(new Transaction
                {
                    SavingsAccountId = primaryAccount.Id,
                    Amount = dto.AdditionalAmount.Value,
                    TransactionType = TransactionType.Credito,
                    OperationType = OperationType.Deposito,
                    Origin = "Ajuste administrativo",
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    PerformedByUserId = DomainConstants.SystemUserId,
                    Channel = ChannelPayment.Administrador,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = DomainConstants.SystemUserId
                });
                await _transactionRepository.SaveChangesAsync();
            }

            return response;
        }

        public async Task<PagedResponseDto<UserDto>> GetCommerceUsersAsync(int page, int pageSize)
        {
            _logger.LogInformation("Obteniendo usuarios con rol Comercio. Página: {Page}", page);

            var usersInRole = await _userManager.GetUsersInRoleAsync(Roles.Comercio.ToString());

            var dtos = usersInRole
                .OrderByDescending(u => u.CreatedAt)
                .Select(user =>
                {
                    var dto = _mapper.Map<UserDto>(user);
                    dto.TypeUser = Roles.Comercio;
                    return dto;
                })
                .ToList();

            return Paginate(dtos, page, pageSize);
        }

        #region Helpers

        //Paginación obligatoria: nunca más de 20 registros por página
        private static PagedResponseDto<UserDto> Paginate(List<UserDto> dtos, int page, int pageSize)
        {
            var currentPage = page < 1 ? 1 : page;
            var limit = pageSize < 1 ? DomainConstants.DefaultPageSize
                      : pageSize > DomainConstants.MaxPageSize ? DomainConstants.MaxPageSize
                      : pageSize;

            return new PagedResponseDto<UserDto>
            {
                Items = dtos.Skip((currentPage - 1) * limit).Take(limit).ToList(),
                TotalCount = dtos.Count,
                Page = currentPage,
                PageSize = limit
            };
        }

        private UserDto BuildUserDto(ApplicationUser user, IList<string> roles)
        {
            var dto = _mapper.Map<UserDto>(user);
            if (Enum.TryParse<Roles>(roles.FirstOrDefault(), true, out var roleEnum))
                dto.TypeUser = roleEnum;
            return dto;
        }

        private async Task<UserOperationResponseDto> ApplyStatusAsync(ApplicationUser user, bool status)
        {
            //El administrador autenticado no puede modificar el estado de su propia cuenta
            if (IsCurrentUser(user.Id))
            {
                _logger.LogWarning("Intento de modificar el estado de la propia cuenta: {UserId}", user.Id);
                return Failure("No puede modificar el estado de su propia cuenta.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            //El rol Comercio queda excluido de forma permanente del mantenimiento de usuarios
            if (roles.Contains(Roles.Comercio.ToString()))
                return NotFoundResponse();

            if (user.IsActive == status)
                return new UserOperationResponseDto();

            user.IsActive = status;
            //Inactivar impide el inicio de sesión; activar deja la cuenta lista para usarse
            user.EmailConfirmed = status;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Failure(result.Errors.FirstOrDefault()?.Description
                    ?? "No fue posible actualizar el estado del usuario.");

            _logger.LogInformation("Estado del usuario {UserId} actualizado a {Status}", user.Id, status);
            return new UserOperationResponseDto();
        }

        private async Task<UserExistenceDto> BuildExistenceAsync(ApplicationUser? user)
        {
            if (user == null) return new UserExistenceDto { Exists = false, IsActive = false };

            var roles = await _userManager.GetRolesAsync(user);
            //El rol Comercio queda excluido de forma permanente
            if (roles.Contains(Roles.Comercio.ToString()))
                return new UserExistenceDto { Exists = false, IsActive = false };

            return new UserExistenceDto { Exists = true, IsActive = user.IsActive };
        }

        //La presentación ya no filtra el identificador: un Id ausente se trata como usuario
        //inexistente en lugar de reventar en el UserManager.
        private async Task<ApplicationUser?> FindUserAsync(string userId)
            => string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);

        //Sin sesión no hay cuenta propia que proteger: las reglas del mantenimiento no aplican
        private bool IsCurrentUser(string userId)
        {
            var currentUserId = _currentUserService.UserId;
            return !string.IsNullOrWhiteSpace(currentUserId)
                && string.Equals(userId, currentUserId, StringComparison.Ordinal);
        }

        private static UserOperationResponseDto Failure(string error)
            => new() { HasError = true, Error = error };

        private static UserOperationResponseDto Conflict(string error)
            => new() { HasError = true, Conflict = true, Error = error };

        private static UserOperationResponseDto NotFoundResponse()
            => new() { HasError = true, NotFound = true, Error = "El usuario seleccionado no existe." };

        #endregion
    }
}
