using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.DTOs.Common
{
    public class PagedResponseDto<T>
    {
        public required List<T> Items { get; set; }
        public required int TotalCount { get; set; }

        //Página solicitada y tamaño realmente aplicado. El tamaño puede diferir del pedido:
        //los listados administrativos nunca devuelven más de 20 registros por página.
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
