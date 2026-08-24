using System;

namespace BookManagement.Service.Book
{
    public class FilterRequest
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? ShopId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Author { get; set; }
        /// <summary>Values: price_asc | price_desc | newest | rating</summary>
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
