using System;

namespace ArtemisBankingPro.Core.Application.DTOs.Cajero
{
    public class TransaccionDto
    {
        public string TipoTransaccion { get; set; }
        public decimal Monto { get; set; }
        public string Origen { get; set; }
        public string Beneficiario { get; set; }
        public string Estado { get; set; }
        public string UsuarioResponsable { get; set; }
        public DateTime Fecha { get; set; }
    }
}
