using System;
using System.Linq;
using System.Threading.Tasks;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Feedback;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _db;

    public FeedbackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedFeedbackResponse> GetShopFeedbacksAsync(int shopId, int? rating, bool? hasResponse, int pageIndex, int pageSize)
    {
        var q = _db.Feedbacks.Where(f => f.ShopId == shopId);

        if (rating.HasValue)
            q = q.Where(f => f.Rating == rating.Value);

        if (hasResponse.HasValue)
            q = hasResponse.Value ? q.Where(f => f.Response != null) : q.Where(f => f.Response == null);

        var totalItems = await q.CountAsync();
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackResponse
            {
                FeedbackId = f.FeedbackId,
                OrderDetailId = f.OrderDetailId,
                ShopId = f.ShopId,
                Rating = f.Rating,
                Content = f.Content,
                Type = f.Type.ToString(),
                ImageUrl = f.ImageUrl,
                CreatedAt = f.CreatedAt,
                BookTitle = f.OrderDetail != null && f.OrderDetail.Book != null ? f.OrderDetail.Book.Title : null,
                Response = f.Response != null ? new FeedbackResponseData
                {
                    ResponseId = f.Response.ResponseId,
                    Content = f.Response.Content,
                    ImageUrl = f.Response.ImageUrl,
                    CreatedAt = f.Response.CreatedAt
                } : null
            })
            .ToListAsync();

        return new PagedFeedbackResponse
        {
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<FeedbackReplyCreatedResponse> CreateFeedbackResponseAsync(int shopId, int feedbackId, CreateFeedbackResponseRequest dto)
    {
        var feedback = await _db.Feedbacks
            .Include(f => f.Response)
            .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.ShopId == shopId);

        if (feedback == null)
            throw new KeyNotFoundException("Feedback not found or unauthorized access.");

        if (feedback.Response != null)
            throw new InvalidOperationException("Response already exists for this feedback.");

        var response = new Response
        {
            FeedbackId = feedbackId,
            ShopId = shopId,
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.Responses.Add(response);
        await _db.SaveChangesAsync();

        return new FeedbackReplyCreatedResponse
        {
            ResponseId = response.ResponseId,
            FeedbackId = response.FeedbackId,
            ShopId = response.ShopId,
            Content = response.Content,
            ImageUrl = response.ImageUrl,
            CreatedAt = response.CreatedAt
        };
    }

    public async Task ProcessReturnRequestAsync(int shopId, int returnRequestId, ProcessReturnRequest dto)
    {
        var returnReq = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId && r.OrderDetail.Book.ShopId == shopId);

        if (returnReq == null)
            throw new KeyNotFoundException("Return request not found or unauthorized.");

        if (dto.Status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            returnReq.Status = ReturnRequestStatus.APPROVED;
            returnReq.OrderDetail.ReturnStatus = ReturnStatus.PROCESSING;
        }
        else if (dto.Status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            returnReq.Status = ReturnRequestStatus.REJECTED;
            returnReq.OrderDetail.ReturnStatus = ReturnStatus.REJECTED;
        }
        else
        {
            throw new ArgumentException("Status must be APPROVED or REJECTED.");
        }

        returnReq.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
