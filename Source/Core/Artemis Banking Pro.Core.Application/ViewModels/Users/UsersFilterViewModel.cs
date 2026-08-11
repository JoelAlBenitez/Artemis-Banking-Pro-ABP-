using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Users
{
    //Filtros del listado del mantenimiento de usuarios. Sin rol seleccionado se listan
    //todos los usuarios; el rol Comercio nunca se ofrece ni se acepta.
    public sealed class UsersFilterViewModel
    {
        [EnumDataType(typeof(Roles), ErrorMessage = "Debe seleccionar un tipo de usuario valido.")]
        public Roles? Role { get; set; }

        [EnumDataType(typeof(StatusFilter), ErrorMessage = "Debe seleccionar un estado valido.")]
        public StatusFilter Status { get; set; } = StatusFilter.Todos;

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una página valida.")]
        public int Page { get; set; } = 1;

        //Ningún listado administrativo devuelve más de 20 registros por página. Sin setter
        //no se enlaza desde la petición: el tamaño no se negocia desde la pantalla.
        public int PageSize => DomainConstants.DefaultPageSize;
    }
}
