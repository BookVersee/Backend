namespace BookManagement.Service.Admin;

public interface IAdminService
{
    // User Management
    Task<PagedResult<UserResponse>> GetUsersAsync(UserFilterRequest filter);
    Task<UserDetailResponse> GetUserDetailAsync(Guid userId);
    Task UpdateUserStatusAsync(Guid userId, string status);

    // Dispute/Return Request Management
    Task<IEnumerable<DisputeResponse>> GetDisputesAsync(string? status = null);
    Task<DisputeResponse> GetDisputeDetailAsync(Guid disputeId);
    Task ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request);

    // Order Monitoring
    Task<PagedResult<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 10);
    Task<PagedResult<OrderResponse>> GetOrdersByStatusAsync(string status, int page = 1, int pageSize = 10);
    Task<OrderResponse> GetOrderDetailAsync(Guid orderId);

    // Book Management
    Task<PagedResult<BookResponse>> GetAllBooksAsync(int page = 1, int pageSize = 10);
    Task<PagedResult<BookResponse>> GetBooksByStatusAsync(string status, int page = 1, int pageSize = 10);
    Task HideBookAsync(Guid bookId);

    // Shop Management & Approval
    Task<IEnumerable<ShopResponse>> GetPendingShopsAsync();
    Task<PagedResult<ShopResponse>> GetAllShopsAsync(int page = 1, int pageSize = 10);
    Task ApproveShopAsync(Guid shopId);
    Task LockShopAsync(Guid shopId, LockShopRequest request);

    // Dashboard & Statistics
    Task<DashboardStatisticsResponse> GetDashboardStatisticsAsync(string period = "month");
    Task<RevenueReportResponse> GetRevenueReportAsync(string period = "month");
    Task<IEnumerable<TopSellingBooksResponse>> GetTopSellingBooksAsync(int limit = 10);

    // Delivery Monitoring
    Task<PagedResult<DeliveryResponse>> GetDeliveriesAsync(string? status, int page = 1, int pageSize = 10);
    Task<DeliveryResponse> GetDeliveryDetailAsync(Guid deliveryId);
}

