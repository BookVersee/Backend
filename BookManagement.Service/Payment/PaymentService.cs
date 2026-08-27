using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using PaymentEntity = BookManagement.Repository.Entities.Payment;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Payment;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly VnpayService _vnpayService;

    public PaymentService(AppDbContext db, VnpayService vnpayService)
    {
        _db = db;
        _vnpayService = vnpayService;
    }

    public async Task<string> CreateVnpayUrlAsync(Guid userId, CreateVnpayUrlDto dto, string ipAddress)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        // 1. Tái sử dụng bản ghi Payment ONLINE PENDING đã tồn tại (Tránh tạo bản ghi trùng lặp)
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

        var paymentUrl = _vnpayService.CreatePaymentUrl(payment.Id, payment.Amount, ipAddress, dto.BankCode);
        return paymentUrl;
    }

    public async Task<(string RspCode, string Message)> ProcessVnpayIpnAsync(IDictionary<string, string> queryParams)
    {
        bool isValidSignature = _vnpayService.ValidateSignature(queryParams);
        if (!isValidSignature)
        {
            return ("97", "Invalid Signature");
        }

        if (!queryParams.TryGetValue("vnp_TxnRef", out var txnRefStr) || !Guid.TryParse(txnRefStr, out Guid paymentId))
        {
            return ("01", "Order Not Found");
        }

        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
        {
            return ("01", "Order Not Found");
        }

        if (payment.Status == PaymentStatus.SUCCESS || payment.Status == PaymentStatus.FAILED)
        {
            return ("02", "Order already confirmed");
        }

        string responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : "99";
        decimal amount = 0m;
        if (queryParams.TryGetValue("vnp_Amount", out var amountStr) && decimal.TryParse(amountStr, out var rawAmount))
        {
            amount = rawAmount / 100m;
        }

        // 5. Kiểm tra đối soát số tiền khớp chuẩn VNPAY
        if (amount > 0 && payment.Amount > 0 && Math.Abs(amount - payment.Amount) > 0.01m)
        {
            return ("04", "Invalid Amount");
        }

        if (responseCode == "00")
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
                    Amount = amount > 0 ? amount : payment.Amount,
                    TransactionCode = queryParams.TryGetValue("vnp_TransactionNo", out var tNo) ? tNo : Guid.NewGuid().ToString("N").Substring(0, 10),
                    Description = $"VNPAY Payment for Order #{order.Id}",
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
        return ("00", "Confirm Success");
    }

    public async Task ProcessVnpayRefundAsync(Guid shopId, VnpayRefundDto dto)
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

        bool refundSuccess = await _vnpayService.ProcessRefundAsync(returnReqId, refundAmount, dto.TransactionNo ?? ("REF" + DateTime.UtcNow.Ticks), "SHOP");

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
                TransactionCode = dto.TransactionNo ?? ("REF" + Guid.NewGuid().ToString("N").Substring(0, 10)),
                Description = dto.RefundReason ?? $"Refund for Order #{dto.OrderId}",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.TransactionHistories.Add(transaction);
            await _db.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("Failed to process VNPAY refund.");
        }
    }
}
