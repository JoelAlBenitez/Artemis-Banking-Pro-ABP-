namespace Artemis_Banking_Pro.Core.Application.ViewModels.Users
{
    //Modelo de la pantalla del listado: filtro aplicado, roles del combo, usuarios de la
    //página y paginación.
    public sealed class UsersListViewModel
    {
        public required UsersFilterViewModel Filter { get; set; }
        public required IReadOnlyCollection<string> AvailableRoles { get; set; }
        public required IReadOnlyCollection<UserViewModel> Users { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalRecords { get; set; }
        public required int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
