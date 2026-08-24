using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TransactionHistory>> GetTransactionsByUserIdAsync(Guid userId)
        {
            return await _context.TransactionHistories
                .Where(th => th.UserId == userId)
                .OrderByDescending(th => th.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CreateTransactionAsync(TransactionHistory transaction)
        {
            await _context.TransactionHistories.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
