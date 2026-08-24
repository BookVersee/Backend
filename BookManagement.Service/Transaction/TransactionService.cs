using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Transaction;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IEnumerable<TransactionHistoryResponse>> GetTransactionsByUserAsync(Guid userId)
    {
        var transactions =
            await _transactionRepository.GetTransactionsByUserIdAsync(userId);

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<decimal> GetUserBalanceAsync(Guid userId)
    {
        var transactions =
            await _transactionRepository.GetTransactionsByUserIdAsync(userId);

        var income = transactions
            .Where(t => t.TransactionType.ToString() == "IN")
            .Sum(t => t.Amount);

        var expense = transactions
            .Where(t => t.TransactionType.ToString() == "OUT")
            .Sum(t => t.Amount);

        return income - expense;
    }

    private static TransactionHistoryResponse MapToResponse(BookManagement.Repository.Entities.TransactionHistory transaction)
    {
        return new TransactionHistoryResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            ReferenceType = transaction.ReferenceType.ToString(),
            ReferenceId = transaction.ReferenceId,
            TransactionType = transaction.TransactionType.ToString(),
            Amount = transaction.Amount,
            TransactionCode = transaction.TransactionCode,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }
}
