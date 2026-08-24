using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ReturnRequest)
                .Include(o => o.Deliveries)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId, OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Include(o => o.Deliveries)
                .Where(o => o.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public Task<IQueryable<Order>> GetQueryableAsync()
        {
            return Task.FromResult(_context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Include(o => o.Deliveries)
                .AsNoTracking());
        }

        public async Task CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task CreateReturnRequestAsync(ReturnRequest returnRequest)
        {
            await _context.ReturnRequests.AddAsync(returnRequest);
            await _context.SaveChangesAsync();
        }

        public async Task<ReturnRequest?> GetReturnRequestByIdAsync(Guid id)
        {
            return await _context.ReturnRequests
                .Include(rr => rr.OrderDetail)
                    .ThenInclude(od => od.Order)
                        .ThenInclude(o => o.User)
                .Include(rr => rr.OrderDetail)
                    .ThenInclude(od => od.Book)
                        .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(rr => rr.Id == id);
        }

        public async Task UpdateReturnRequestAsync(ReturnRequest returnRequest)
        {
            _context.ReturnRequests.Update(returnRequest);
            await _context.SaveChangesAsync();
        }

        public Task<IQueryable<ReturnRequest>> GetDisputesQueryableAsync()
        {
            return Task.FromResult(_context.ReturnRequests
                .Include(rr => rr.OrderDetail)
                    .ThenInclude(od => od.Order)
                        .ThenInclude(o => o.User)
                .Include(rr => rr.OrderDetail)
                    .ThenInclude(od => od.Book)
                        .ThenInclude(b => b.Shop)
                .AsNoTracking());
        }

        public async Task<OrderDetail?> GetOrderDetailByIdAsync(Guid orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Book)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);
        }
    }
}
