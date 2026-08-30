namespace BookManagement.Service.User
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class SendOtpRequest
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyPasswordOtpRequest
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }

    public class ResetNewPasswordRequest
    {
        public string Email { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class ChangePasswordWithOldPasswordRequest
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class RegisterShopRequest
    {
        public string ShopName { get; set; } = null!;
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }
}
