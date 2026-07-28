using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros
{
    public static class LoandError
    {
        public static readonly Error NonExistsLoans
            = new("Error", "Este cliente no tiene préstamos registrados.");

        public static readonly Error CustomerWithLoanExist =
            new("Error", "Este cliente ya tiene un préstamo activo asignado.");

        public static readonly Error InvalidTerm
            = new("Error", "El plazo seleccionado no es válido.");

        public static readonly Error FailedProcessLoan
            = new("Error", "Ha ocurrido un error imprevisto al procesar el prestamo. Favor intente de nuevo más tarde.");

        public static readonly Error FaildGenerateLoansInstallment
            = new("Error", "La tabla de amortización de este prestamo no pudo ser creada. Favor intente de nuevo más tarde.");

        public static readonly Error NonExistAccountFirstActive
            = new("Error", "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso ndel préstamo.");

    }
}
