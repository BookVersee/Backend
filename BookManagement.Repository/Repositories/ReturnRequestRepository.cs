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
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly AppDbContext _context;

        public ReturnRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnRequest?> GetByIdAsync(Guid returnRequestId)
        {
            return await _context.ReturnRequests
                .Include(r => r.OrderDetail)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);
        }

        public async Task<ReturnRequest?> GetByOrderDetailIdAsync(Guid orderDetailId)
        {
            return await _context.ReturnRequests
                .Include(r => r.OrderDetail)
                .Where(r => r.OrderDetailId == orderDetailId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ReturnRequest>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.OrderDetail)
                .Where(r => r.OrderDetail.Order.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<ReturnRequest>> GetByStatusAsync(string status)
        {
            return await _context.ReturnRequests
                .Include(r => r.OrderDetail)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(ReturnRequest returnRequest)
        {
            await _context.ReturnRequests.AddAsync(returnRequest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ReturnRequest returnRequest)
        {
            _context.ReturnRequests.Update(returnRequest);
            await _context.SaveChangesAsync();
        }
    }
}
