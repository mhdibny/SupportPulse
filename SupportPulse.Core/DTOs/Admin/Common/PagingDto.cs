namespace SupportPulse.Core.DTOs.Admin.Common
{
    /// <summary>
    /// Represents a paged result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public List<T> Items { get; set; } = new();

        /// <summary>Total number of items across all pages.</summary>
        public int TotalCount { get; set; }

        /// <summary>The current page number (1‑based).</summary>
        public int PageNumber { get; set; }

        /// <summary>The number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Calculated total number of pages.</summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}