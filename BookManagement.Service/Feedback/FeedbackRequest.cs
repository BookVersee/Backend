using System.Text.Json.Serialization;

namespace BookManagement.Service.Feedback;

public class CreateFeedbackResponseRequest
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

public class ProcessReturnRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; set; }
}
