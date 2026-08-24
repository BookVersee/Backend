namespace BookManagement.Service.Transaction;

public interface ITransactionService
{
    Task<IEnumerable<TransactionHistoryResponse>> GetTransactionsByUserAsync(Guid userId);
    Task<decimal> GetUserBalanceAsync(Guid userId);
}
