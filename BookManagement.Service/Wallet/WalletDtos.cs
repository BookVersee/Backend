using System;
using System.Collections.Generic;

namespace BookManagement.Service.Wallet
{
    public class WithdrawRequest
    {
        public decimal Amount { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
    }

    public class WalletResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Balance { get; set; }
        public decimal HeldBalance { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class EscrowResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ShopId { get; set; }
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetShopEarnings { get; set; }
        public DateTimeOffset ReleaseDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
