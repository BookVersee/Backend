using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Api.Hubs;

/// <summary>
/// SignalR App Hub: Hub trung tâm quản lý các sự kiện toàn ứng dụng:
/// - Đơn hàng mới cho Shop
/// - Cập nhật trạng thái đơn hàng & Vận chuyển (GHN)
/// - Trạng thái thanh toán (MoMo / VNPay / QR)
/// </summary>
[Authorize]
public class AppHub : Hub
{
    private readonly AppDbContext _db;

    public AppHub(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            // Tự động tham gia group cá nhân của User
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Nếu là Shop hoặc User sở hữu Shop, tự động tham gia group shop_{id}
            var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == userId || s.UserId == userId);
            if (shop != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shop.Id}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{userId}");
            }
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Shop đăng ký lắng nghe group thông báo của Shop mình
    /// </summary>
    public async Task JoinShop(Guid shopId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shopId}");
    }

    /// <summary>
    /// Client lắng nghe realtime cho 1 đơn hàng cụ thể (theo dõi trạng thái thanh toán / giao hàng)
    /// </summary>
    public async Task JoinOrder(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }

    /// <summary>
    /// Rời khỏi kênh theo dõi đơn hàng
    /// </summary>
    public async Task LeaveOrder(string orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }
}
