namespace ArtemisBankingPro.Core.Application.DTOs.Cajero
{
    public class CuentaAhorroDto
    {
        public string NumeroCuenta { get; set; }
        public string NombreTitular { get; set; }
        public bool EstaActiva { get; set; }
        public decimal Balance { get; set; }
    }
}
