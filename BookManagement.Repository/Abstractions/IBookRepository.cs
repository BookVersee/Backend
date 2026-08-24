using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Abstractions
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(Guid id);
        Task<IQueryable<Book>> GetQueryableAsync();
        Task<IEnumerable<Book>> GetBooksByShopIdAsync(Guid shopId);
        Task<Shop?> GetShopByIdAsync(Guid shopId);
        Task UpdateAsync(Book book);
    }
}
