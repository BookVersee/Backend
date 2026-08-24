using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<TransactionHistory>> GetTransactionsByUserIdAsync(Guid userId);
        Task CreateTransactionAsync(TransactionHistory transaction);
    }
}
