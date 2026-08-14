using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Management
{
    //Contratos que el módulo de Administración consume de Identity.
    //Regla transversal: el rol Comercio nunca se lista, no se filtra y no se crea desde aquí.
    public interface IUserManagementService
    {
        // 1 - Listado paginado de usuarios (máx. 20 por página, del más reciente al más antiguo)
        Task<PagedResponseDto<UserDto>> GetUsersAsync(int page, int pageSize, StatusFilter status);
        // 2 - Mismo listado filtrado por rol
        Task<PagedResponseDto<UserDto>> GetUsersByRoleAsync(Roles role, int page, int pageSize);
        // 3 - Roles del sistema, excluyendo Comercio
        Task<List<string>> GetRolesAsync();
        // 5 - Activar / inactivar usuario
        Task<UserOperationResponseDto> ToggleUserAsync(string userId, string currentUserId);
        //Variante usada por la Web API: el estado llega explícito en el body
        Task<UserOperationResponseDto> SetUserStatusAsync(string userId, bool status, string currentUserId);
        // 6 - Detalle de un usuario (consulta y edición)
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        // 7 - Roles asociados a un usuario
        Task<List<string>> GetRolesByUserAsync(string userId);
        // 8 - Datos base del cliente: Id, Name, LastName
        Task<ClientBaseDataDto?> GetClientBaseDataAsync(string userId);
        //Titular en una sola cadena: lo consume el módulo Cajero para mostrar el propietario
        //de la cuenta, la tarjeta o el préstamo sin componer el nombre en cada pantalla.
        Task<string?> GetFullNameByIdAsync(string userId);
        // 9 - Existencia y estado del usuario, por Id o por cédula
        Task<UserExistenceDto> ValidateUserExistsByIdAsync(string userId);
        Task<UserExistenceDto> ValidateUserExistsByIdCardAsync(string idCard);
        // 10 - Clientes activos
        Task<List<ClientSummaryDto>> GetActiveClientsAsync();
        // 11 - Solo los identificadores de los clientes activos
        Task<IReadOnlyCollection<string>> GetActiveClientIdsAsync();
        // 12 - Cliente activo por cédula
        Task<ClientSummaryDto?> GetClientByIdCardAsync(string idCard);
        //Edición de usuario del mantenimiento administrativo
        Task<UserOperationResponseDto> UpdateUserAsync(string id, EditUserDto dto);
        //Exclusivo de la Web API: listado de usuarios con rol Comercio
        Task<PagedResponseDto<UserDto>> GetCommerceUsersAsync(int page, int pageSize);
    }
}
