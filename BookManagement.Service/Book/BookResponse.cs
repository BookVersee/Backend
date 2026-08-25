using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Book;

// === RESPONSE DTOs ===
public class BookResponse
{
    [JsonPropertyName("book_id")]
    public int BookId { get; set; }

    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isbn")]
    public string Isbn { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("published_year")]
    public int PublishedYear { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public float Rating { get; set; }
}

public class PagedBookResponse
{
    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("items")]
    public IEnumerable<BookResponse> Items { get; set; } = new List<BookResponse>();
}
