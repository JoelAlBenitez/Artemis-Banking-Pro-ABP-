using ArtemisBankingPro.Core.Domain.Common.Pagination;

namespace ArtemisBankingPro.Core.Application.DTOs.Common
{
    /// <summary>
    /// Envoltura de todo listado paginado de la Web API.
    /// </summary>
    /// <remarks>
    /// Los nombres de las propiedades son los que el documento funcional muestra en cada
    /// ejemplo de respuesta: la colección viaja como data y el total como totalRecords.
    /// </remarks>
    public class PagedApiResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public IReadOnlyCollection<T> Data { get; set; } = [];

        public static PagedApiResponse<T> From<TSource>(
            PagedResult<TSource> source,
            Func<TSource, T> projection)
            => new()
            {
                Page = source.Page,
                PageSize = source.PageSize,
                TotalRecords = source.TotalRecords,
                Data = source.Items.Select(projection).ToList()
            };
    }
}
