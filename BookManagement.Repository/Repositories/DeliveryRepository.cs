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
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly AppDbContext _context;

        public DeliveryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Delivery?> GetByIdAsync(Guid deliveryId)
        {
            return await _context.Deliveries
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == deliveryId);
        }

        public async Task<Delivery?> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.Deliveries
                .Where(d => d.OrderId == orderId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Delivery>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Deliveries
                .Where(d => d.Order.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Delivery delivery)
        {
            await _context.Deliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Delivery delivery)
        {
            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByTrackingNumberAsync(string trackingNumber)
        {
            return await _context.Deliveries
                .AnyAsync(d => d.TrackingNumber == trackingNumber);
        }
    }
}
