namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    //Resultado de la activación de cuenta. El mensaje se muestra tal cual en la aplicación
    //web; la Web API solo necesita saber si hubo error para responder 204 o 400.
    public class ConfirmAccountResponse
    {
        public bool HasError { get; set; }
        public required string Message { get; set; }
    }
}
