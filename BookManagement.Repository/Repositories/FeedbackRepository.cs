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
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly AppDbContext _context;

        public FeedbackRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByBookIdAsync(Guid bookId)
        {
            return await _context.Feedbacks
                .Include(f => f.Shop)
                .Include(f => f.Response)
                .Include(f => f.OrderDetail)
                .Where(f => f.OrderDetail.BookId == bookId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByShopIdAsync(Guid shopId)
        {
            return await _context.Feedbacks
                .Include(f => f.Response)
                .Include(f => f.OrderDetail)
                    .ThenInclude(od => od.Book)
                .Where(f => f.ShopId == shopId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CreateFeedbackAsync(Feedback feedback)
        {
            await _context.Feedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(Guid id)
        {
            return await _context.Feedbacks
                .Include(f => f.Shop)
                .Include(f => f.Response)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Feedback?> GetFeedbackByOrderDetailIdAsync(Guid orderDetailId)
        {
            return await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.OrderDetailId == orderDetailId);
        }

        public async Task<Response?> GetResponseByFeedbackIdAsync(Guid feedbackId)
        {
            return await _context.Responses
                .FirstOrDefaultAsync(r => r.FeedbackId == feedbackId);
        }
    }
}
