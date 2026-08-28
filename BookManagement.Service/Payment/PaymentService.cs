using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading;
using PaymentEntity = BookManagement.Repository.Entities.Payment;

namespace BookManagement.Service.Payment;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly MomoService _momoService;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _paymentLocks = new();

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

        var semaphore = _paymentLocks.GetOrAdd(paymentId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            var payment = await _db.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId || p.OrderId == paymentId);

            if (payment == null)
            {
                return (1, "Payment Record Not Found");
            }

            // Chống Idempotency: Nếu Payment đã SUCCESS hoặc mã giao dịch MoMo đã ghi nhận
            string transIdStr = req.TransId.ToString();
            bool isTransRecorded = req.TransId > 0 && await _db.TransactionHistories
                .AnyAsync(t => t.TransactionCode == transIdStr && t.ReferenceType == ReferenceType.ORDER_PAYMENT);

            if (payment.Status == PaymentStatus.SUCCESS || isTransRecorded)
            {
                return (0, "Payment already confirmed");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (req.ResultCode == 0)
                {
                    payment.Status = PaymentStatus.SUCCESS;
                    payment.TransactionCode = transIdStr;
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
                            TransactionCode = transIdStr,
                            Description = $"MoMo Payment for Order #{order.Id}",
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        _db.TransactionHistories.Add(transaction);
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.FAILED;
                    payment.TransactionCode = transIdStr;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return (0, "Confirm Success");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        finally
        {
            semaphore.Release();
        }
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

        // Chống Idempotency: Kiểm tra nếu yêu cầu đổi trả đã hoàn tiền
        if (returnReq != null && returnReq.OrderDetail.ReturnStatus == ReturnStatus.REFUNDED)
        {
            throw new InvalidOperationException("Yêu cầu trả hàng này đã được hoàn tiền trước đó.");
        }

        if (returnReq != null)
        {
            bool hasExistingRefundPayment = await _db.Payments
                .AnyAsync(p => p.ReturnRequestId == returnReq.Id && p.PaymentType == PaymentType.REFUND && p.Status == PaymentStatus.SUCCESS);
            if (hasExistingRefundPayment)
            {
                throw new InvalidOperationException("Khoản tiền cho yêu cầu trả hàng này đã được xử lý hoàn trả trước đó.");
            }
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

        // Kiểm tra tổng tiền đã hoàn cho đơn hàng
        var alreadyRefundedAmount = await _db.Payments
            .Where(p => p.OrderId == dto.OrderId && p.PaymentType == PaymentType.REFUND && p.Status == PaymentStatus.SUCCESS)
            .SumAsync(p => p.Amount);

        if (alreadyRefundedAmount + refundAmount > order.TotalAmount)
        {
            throw new InvalidOperationException($"Tổng số tiền hoàn ({alreadyRefundedAmount + refundAmount:N0} đ) vượt quá tổng giá trị đơn hàng ({order.TotalAmount:N0} đ).");
        }

        string transNo = dto.TransactionNo ?? ("MOMO_REF_" + DateTime.UtcNow.Ticks);
        if (!string.IsNullOrEmpty(dto.TransactionNo))
        {
            bool isTransCodeDuplicate = await _db.TransactionHistories
                .AnyAsync(t => t.TransactionCode == dto.TransactionNo && t.ReferenceType == ReferenceType.REFUND);
            if (isTransCodeDuplicate)
            {
                throw new InvalidOperationException("Mã giao dịch hoàn tiền này đã tồn tại.");
            }
        }

        Guid returnReqId = returnReq?.Id ?? Guid.NewGuid();
        bool refundSuccess = await _momoService.ProcessRefundAsync(returnReqId, refundAmount, transNo, "SHOP");

        if (refundSuccess)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var refundPayment = new PaymentEntity
                {
                    OrderId = dto.OrderId,
                    ReturnRequestId = returnReq?.Id,
                    PaymentType = PaymentType.REFUND,
                    Method = PaymentMethod.ONLINE,
                    Amount = refundAmount,
                    Status = PaymentStatus.SUCCESS,
                    TransactionCode = transNo,
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
                    TransactionCode = transNo,
                    Description = dto.RefundReason ?? $"Refund for Order #{dto.OrderId}",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.TransactionHistories.Add(transaction);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to process MoMo refund.");
        }
    }
}

