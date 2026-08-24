using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<Feedback>> GetFeedbacksByBookIdAsync(Guid bookId);
        Task<IEnumerable<Feedback>> GetFeedbacksByShopIdAsync(Guid shopId);
        Task CreateFeedbackAsync(Feedback feedback);
        Task<Feedback?> GetFeedbackByIdAsync(Guid id);
        Task<Feedback?> GetFeedbackByOrderDetailIdAsync(Guid orderDetailId);
        Task<Response?> GetResponseByFeedbackIdAsync(Guid feedbackId);
    }
}
