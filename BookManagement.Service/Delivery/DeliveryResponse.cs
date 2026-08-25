using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Delivery;

public class DeliveryItemResponse
{
    [JsonPropertyName("book_id")]
    public int BookId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("return_status")]
    public string ReturnStatus { get; set; } = string.Empty;
}

public class DeliveryManifestResponse
{
    [JsonPropertyName("delivery_id")]
    public int DeliveryId { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("ship_fee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("recipient_name")]
    public string RecipientName { get; set; } = string.Empty;

    [JsonPropertyName("recipient_phone")]
    public string RecipientPhone { get; set; } = string.Empty;

    [JsonPropertyName("recipient_address")]
    public string RecipientAddress { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("cod_amount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("items")]
    public List<DeliveryItemResponse> Items { get; set; } = new();
}

public class PagedDeliveryResponse
{
    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("items")]
    public IEnumerable<object> Items { get; set; } = new List<object>();
}
