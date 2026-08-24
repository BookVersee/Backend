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
    public class ResponseRepository : IResponseRepository
    {
        private readonly AppDbContext _context;

        public ResponseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Response?> GetByIdAsync(Guid responseId)
        {
            return await _context.Responses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == responseId);
        }

        public async Task<Response?> GetByFeedbackIdAsync(Guid feedbackId)
        {
            return await _context.Responses
                .Where(r => r.FeedbackId == feedbackId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Response>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Responses
                .Where(r => r.ShopId == shopId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Response response)
        {
            await _context.Responses.AddAsync(response);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Response response)
        {
            _context.Responses.Update(response);
            await _context.SaveChangesAsync();
        }
    }
}
