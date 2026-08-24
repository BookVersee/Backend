using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Chat;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;

    public ChatService(IChatRepository chatRepository, IMessageRepository messageRepository)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
    }

    public async Task<ChatResponse> GetChatAsync(Guid chatId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        if (chat == null)
            throw new Exception("Chat not found");
        return MapToChatResponse(chat);
    }

    public async Task<ChatResponse> GetOrCreateChatAsync(Guid userId, Guid shopId)
    {
        var chat = await _chatRepository.GetByUserAndShopAsync(userId, shopId);
        if (chat != null)
            return MapToChatResponse(chat);

        var newChat = new BookManagement.Repository.Entities.Chat
        {
            UserId = userId,
            ShopId = shopId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _chatRepository.AddAsync(newChat);
        return MapToChatResponse(newChat);
    }

    public async Task<IEnumerable<ChatResponse>> GetChatsByUserAsync(Guid userId)
    {
        var chats = await _chatRepository.GetChatsByUserAsync(userId);
        return chats.Select(MapToChatResponse).ToList();
    }

    public async Task<MessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        var message = new BookManagement.Repository.Entities.Message
        {
            ChatId = request.ChatId,
            SenderId = request.SenderId,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _messageRepository.AddAsync(message);
        return MapToMessageResponse(message);
    }

    public async Task MarkMessagesAsReadAsync(Guid chatId, Guid userId)
    {
        await _messageRepository.MarkChatMessagesAsReadAsync(chatId);
    }

    private static ChatResponse MapToChatResponse(BookManagement.Repository.Entities.Chat chat)
    {
        return new ChatResponse
        {
            Id = chat.Id,
            UserId = chat.UserId,
            ShopId = chat.ShopId,
            ShopName = chat.Shop?.ShopName ?? "",
            UpdatedAt = chat.UpdatedAt ?? chat.CreatedAt,
            Messages = chat.Messages?.Select(MapToMessageResponse).ToList() ?? new List<MessageResponse>()
        };
    }

    private static MessageResponse MapToMessageResponse(BookManagement.Repository.Entities.Message message)
    {
        return new MessageResponse
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? "",
            Content = message.Content,
            ImageUrl = message.ImageUrl,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }
}
