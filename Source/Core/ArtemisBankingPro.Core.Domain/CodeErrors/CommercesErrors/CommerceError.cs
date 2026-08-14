using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors
{
    public static class CommerceError
    {
        public static readonly Error NonExistsCommerce
            = new("Error", "El comercio seleccionado no existe.");

        public static readonly Error RncAlreadyRegistered
            = new("Error", "Ya existe un comercio registrado con este RNC.");

        public static readonly Error EmailAlreadyRegistered
            = new("Error", "Ya existe un comercio registrado con este correo electrónico.");

        public static readonly Error CommerceAlreadyHasUser
            = new("Error", "Este comercio ya tiene un usuario asociado.");

        public static readonly Error CommerceIsNotActive
            = new("Error", "El comercio se encuentra inactivo.");

        public static readonly Error CommerceWithoutAssociatedUser
            = new("Error", "El comercio no tiene un usuario asociado.");

        public static readonly Error AdminUserRequired
            = new("Error", "No fue posible identificar al administrador responsable de la operación.");

        public static readonly Error FailedProcessCommerce
            = new("Error", "Ha ocurrido un error imprevisto al procesar el comercio. Favor intente de nuevo más tarde.");

        //Al desactivar un comercio sus usuarios se inactivan en Identity. Los dos contextos no
        //comparten transacción: si el segundo paso falla se informa como advertencia y queda
        //registrado en Serilog.
        public static readonly Error CommerceStatusChangedWithoutUsersUpdate
            = new("Advertencia", "El estado del comercio fue actualizado, pero no fue posible actualizar el estado de sus usuarios asociados.");
    }
}
