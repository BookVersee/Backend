using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _dbContext;

    public ChatHub(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task JoinChat(int chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task LeaveChat(int chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task SendMessage(int chatId, string content, string? imageUrl)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("user_id")?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out int senderId))
        {
            throw new HubException("User is not authenticated");
        }

        var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId);
        if (chat == null)
        {
            throw new HubException("Chat session not found");
        }

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            ImageUrl = imageUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        chat.UpdatedAt = DateTime.UtcNow;
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        var messageDto = new MessageDto
        {
            MessageId = message.MessageId,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            Content = message.Content,
            ImageUrl = message.ImageUrl,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };

        await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", messageDto);
    }
}
