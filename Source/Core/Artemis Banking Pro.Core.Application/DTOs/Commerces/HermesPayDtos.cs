using ArtemisBankingPro.Core.Application.DTOs.Common;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Commerces
{
    /// <summary>
    /// Pago recibido por un comercio a través de Hermes Pay.
    /// </summary>
    /// <remarks>El único dato de la tarjeta expuesto son sus últimos cuatro dígitos.</remarks>
    public sealed class CommercePaymentDto
    {
        public int Id { get; set; }
        public DateTimeOffset TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public required string CardLastFourDigits { get; set; }

        //APROBADO o RECHAZADO
        public required string Status { get; set; }
    }

    /// <summary>
    /// Listado paginado de transacciones de un comercio, con la identificación del comercio.
    /// </summary>
    public sealed class CommercePaymentsPageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public int CommerceId { get; set; }
        public required string CommerceName { get; set; }
        public IReadOnlyCollection<CommercePaymentDto> Data { get; set; } = [];

        public static CommercePaymentsPageDto From(
            PagedApiResponse<CommercePaymentDto> page, int commerceId, string commerceName)
            => new()
            {
                Page = page.Page,
                PageSize = page.PageSize,
                TotalRecords = page.TotalRecords,
                CommerceId = commerceId,
                CommerceName = commerceName,
                Data = page.Data
            };
    }
}
