using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors
{
    public static class GeneralError
    {
        public static readonly Error UnexpectedError =
            new ("Error", "Ha ocurrido un error inesperado al procesar la solicitud. Favor Intente de nuevo más tarde.");

        public static readonly Error NonExistence =
            new("Error", "El registro seleccionado no existe/");

        public static readonly Error DataInvalid
          = new("Error", "Datos invalidos. Favor intentar de nuevo más tarde.");

    }
}
