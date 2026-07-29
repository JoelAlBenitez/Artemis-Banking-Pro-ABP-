namespace ArtemisBankingPro.Core.Domain.Common.Pagination
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyCollection<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalRecords { get; }
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalRecords / (double)PageSize);

        public PagedResult(IReadOnlyCollection<T> items, int page, int pageSize, int totalRecords)
        {
            Items = items ?? new List<T>();
            Page = page;
            PageSize = pageSize;
            TotalRecords = totalRecords;
        }

        public static PagedResult<T> Empty(int page, int pageSize)
            => new PagedResult<T>(new List<T>(), page, pageSize, 0);
    }
}
