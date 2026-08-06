using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.DTOs.Common
{
    public class PagedResponseDto<T>
    {
        public required List<T> Items { get; set; }
        public required int TotalCount { get; set; }
    }
}
