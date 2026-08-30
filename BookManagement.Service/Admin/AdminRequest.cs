namespace BookManagement.Service.Admin;

public class UserFilterRequest
{
    public BookManagement.Repository.Entities.Enums.UserRole? Role { get; set; }
    public BookManagement.Repository.Entities.Enums.UserStatus? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UpdateUserStatusRequest
{
    public BookManagement.Repository.Entities.Enums.UserStatus Status { get; set; }
}

public class ResolveDisputeRequest
{
    public bool ApproveRefund { get; set; }
    public required string AdminResolutionNote { get; set; }
}

public class LockShopRequest
{
    public required string Reason { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public required string Status { get; set; }
}

public class CreateAdminRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public BookManagement.Repository.Entities.Enums.UserRole Role { get; set; } = BookManagement.Repository.Entities.Enums.UserRole.ADMIN;
}
