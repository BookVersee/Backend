using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Wallet
{
    public interface IWalletService
    {
        Task<WalletResponseDto> GetOrCreateWalletAsync(Guid userId);
        Task<IEnumerable<EscrowResponseDto>> GetShopEscrowsAsync(Guid shopId);
        Task CreateEscrowForOrderAsync(Guid orderId, Guid shopId, decimal amount, decimal platformFeeRate = 0.05m);
        Task ReleaseEscrowAsync(Guid orderId);
        Task RefundEscrowAsync(Guid orderId);
        Task WithdrawAsync(Guid userId, WithdrawRequest request);
    }
}
