namespace BookManagement.Service.Chat;

public interface IChatService
{
    Task<ChatResponse> GetChatAsync(Guid chatId);
    Task<ChatResponse> GetOrCreateChatAsync(Guid userId, Guid shopId);
    Task<IEnumerable<ChatResponse>> GetChatsByUserAsync(Guid userId);
    Task<MessageResponse> SendMessageAsync(SendMessageRequest request);
    Task MarkMessagesAsReadAsync(Guid chatId, Guid userId);
}
