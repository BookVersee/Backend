using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = BookManagement.Repository.Entities.Payment;

namespace BookManagement.Service.Payment;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly MomoService _momoService;

    public PaymentService(AppDbContext db, MomoService momoService)
    {
        _db = db;
        _momoService = momoService;
    }

    public async Task<(string PaymentUrl, string? QrCodeUrl, string? Deeplink)> CreateMomoUrlAsync(Guid userId, CreatePaymentUrlDto dto, string ipAddress)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        // Tái sử dụng hoặc tạo mới bản ghi Payment PENDING
        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == dto.OrderId && p.Method == PaymentMethod.ONLINE && p.Status == PaymentStatus.PENDING);

        PaymentEntity payment;
        if (existingPayment != null)
        {
            payment = existingPayment;
            payment.Amount = order.TotalAmount;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            payment = new PaymentEntity
            {
                OrderId = dto.OrderId,
                PaymentType = PaymentType.PAYMENT,
                Method = PaymentMethod.ONLINE,
                Amount = order.TotalAmount,
                Status = PaymentStatus.PENDING,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Payments.Add(payment);
        }

        await _db.SaveChangesAsync();

        string orderInfo = !string.IsNullOrEmpty(dto.OrderInfo) ? dto.OrderInfo : $"Thanh toan don hang {order.Id}";
        return await _momoService.CreatePaymentAsync(payment.Id, payment.Amount, orderInfo);
    }

    public async Task<(int ResultCode, string Message)> ProcessMomoIpnAsync(MomoIpnRequest req)
    {
        bool isValidSignature = _momoService.ValidateIpnSignature(req);
        if (!isValidSignature)
        {
            return (97, "Invalid Signature");
        }

        string rawId = req.OrderId.Split('_')[0];
        if (!Guid.TryParse(rawId, out Guid paymentId))
        {
            return (1, "Order Not Found");
        }

        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId || p.OrderId == paymentId);

        if (payment == null)
        {
            return (1, "Payment Record Not Found");
        }

        if (payment.Status == PaymentStatus.SUCCESS)
        {
            return (0, "Payment already confirmed");
        }

        if (req.ResultCode == 0)
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            var order = payment.Order;
            if (order != null)
            {
                order.OrderStatus = OrderStatus.PAID;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                var transaction = new TransactionHistory
                {
                    UserId = order.UserId,
                    ReferenceType = ReferenceType.ORDER_PAYMENT,
                    ReferenceId = order.Id,
                    TransactionType = TransactionType.IN,
                    Amount = req.Amount > 0 ? (decimal)req.Amount : payment.Amount,
                    TransactionCode = req.TransId.ToString(),
                    Description = $"MoMo Payment for Order #{order.Id}",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.TransactionHistories.Add(transaction);
            }
        }
        else
        {
            payment.Status = PaymentStatus.FAILED;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (0, "Confirm Success");
    }

    public async Task ProcessRefundAsync(Guid shopId, ProcessRefundDto dto)
    {
        ReturnRequest? returnReq = null;

        if (dto.ReturnRequestId.HasValue && dto.ReturnRequestId.Value != Guid.Empty)
        {
            returnReq = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Book)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(r => r.Id == dto.ReturnRequestId.Value && r.OrderDetail.OrderId == dto.OrderId);
        }
        else
        {
            returnReq = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Book)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(r => r.OrderDetail.OrderId == dto.OrderId);
        }

        var order = await _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        if (shopId != Guid.Empty && !order.OrderDetails.Any(od => od.Book != null && od.Book.ShopId == shopId))
        {
            throw new UnauthorizedAccessException("Shop does not have permission to refund this order.");
        }

        decimal itemRefundFallback = (returnReq?.OrderDetail != null) ? (returnReq.OrderDetail.UnitPrice * returnReq.OrderDetail.Quantity) : order.TotalAmount;
        decimal refundAmount = dto.Amount ?? ((returnReq?.RefundAmount > 0) ? returnReq.RefundAmount : itemRefundFallback);
        Guid returnReqId = returnReq?.Id ?? Guid.NewGuid();

        bool refundSuccess = await _momoService.ProcessRefundAsync(returnReqId, refundAmount, dto.TransactionNo ?? ("MOMO_REF_" + DateTime.UtcNow.Ticks), "SHOP");

        if (refundSuccess)
        {
            var refundPayment = new PaymentEntity
            {
                OrderId = dto.OrderId,
                ReturnRequestId = returnReq?.Id,
                PaymentType = PaymentType.REFUND,
                Method = PaymentMethod.ONLINE,
                Amount = refundAmount,
                Status = PaymentStatus.SUCCESS,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Payments.Add(refundPayment);

            if (returnReq != null)
            {
                returnReq.OrderDetail.ReturnStatus = ReturnStatus.REFUNDED;
            }

            var transaction = new TransactionHistory
            {
                UserId = order.UserId,
                ReferenceType = ReferenceType.REFUND,
                ReferenceId = returnReq?.Id ?? order.Id,
                TransactionType = TransactionType.OUT,
                Amount = refundAmount,
                TransactionCode = dto.TransactionNo ?? ("MOMO_REF_" + Guid.NewGuid().ToString("N").Substring(0, 10)),
                Description = dto.RefundReason ?? $"Refund for Order #{dto.OrderId}",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.TransactionHistories.Add(transaction);
            await _db.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("Failed to process MoMo refund.");
        }
    }
}

