using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros
{
    public static class BeneficiaryError
    {
        public static readonly Error AccountNotFound = new(
            "Beneficiary.AccountNotFound",
            "El número de cuenta ingresado no corresponde a una cuenta válida."
        );

        public static readonly Error AccountCanceled = new(
            "Beneficiary.AccountCanceled",
            "No puede agregar una cuenta cancelada como beneficiario."
        );

        public static readonly Error OwnAccount = new(
            "Beneficiary.OwnAccount",
            "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas."
        );

        public static readonly Error AlreadyRegistered = new(
            "Beneficiary.AlreadyRegistered",
            "Esta cuenta ya se encuentra registrada como beneficiario."
        );

        public static readonly Error BeneficiaryNotFound = new(
            "Beneficiary.NotFound",
            "El beneficiario especificado no existe o no pertenece a este cliente."
        );
    }
}
