using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Services;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly VnpayService _vnpayService;

    public PaymentService(AppDbContext db, VnpayService vnpayService)
    {
        _db = db;
        _vnpayService = vnpayService;
    }

    public async Task<string> CreateVnpayUrlAsync(int userId, CreateVnpayUrlDto dto, string ipAddress)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        var payment = new Payment
        {
            OrderId = dto.OrderId,
            PaymentType = PaymentType.PAYMENT,
            Method = PaymentMethod.ONLINE,
            Amount = order.TotalAmount,
            Status = PaymentStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var paymentUrl = _vnpayService.CreatePaymentUrl(payment.PaymentId, payment.Amount, ipAddress, dto.BankCode);
        return paymentUrl;
    }

    public async Task<(string RspCode, string Message)> ProcessVnpayIpnAsync(IDictionary<string, string> queryParams)
    {
        bool isValidSignature = _vnpayService.ValidateSignature(queryParams);
        if (!isValidSignature)
        {
            return ("97", "Invalid Signature");
        }

        if (!queryParams.TryGetValue("vnp_TxnRef", out var txnRefStr) || !int.TryParse(txnRefStr, out int paymentId))
        {
            return ("01", "Order Not Found");
        }

        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

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

        if (responseCode == "00")
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.UpdatedAt = DateTime.UtcNow;

            var order = payment.Order;
            if (order != null)
            {
                order.OrderStatus = OrderStatus.PAID;
                order.UpdatedAt = DateTime.UtcNow;

                var transaction = new TransactionHistory
                {
                    UserId = order.UserId,
                    ReferenceType = TransactionReferenceType.ORDER_PAYMENT,
                    ReferenceId = order.OrderId,
                    TransactionType = TransactionType.IN,
                    Amount = amount > 0 ? amount : payment.Amount,
                    TransactionCode = queryParams.TryGetValue("vnp_TransactionNo", out var tNo) ? tNo : Guid.NewGuid().ToString("N")[..10],
                    Description = $"VNPAY Payment for Order #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow
                };

                _db.TransactionHistories.Add(transaction);
            }
        }
        else
        {
            payment.Status = PaymentStatus.FAILED;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return ("00", "Confirm Success");
    }

    public async Task ProcessVnpayRefundAsync(int shopId, VnpayRefundDto dto)
    {
        var returnReq = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.Book)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.Order)
            .FirstOrDefaultAsync(r => r.ReturnRequestId == dto.ReturnRequestId && r.OrderDetail.OrderId == dto.OrderId);

        if (returnReq == null)
        {
            throw new KeyNotFoundException("Return request not found.");
        }

        if (returnReq.OrderDetail.Book.ShopId != shopId)
        {
            throw new InvalidOperationException("Unauthorized shop for this return request.");
        }

        if (returnReq.Status != ReturnRequestStatus.APPROVED)
        {
            throw new InvalidOperationException("Return request must be APPROVED before processing refund.");
        }

        var order = returnReq.OrderDetail.Order;
        bool refundSuccess = await _vnpayService.ProcessRefundAsync(dto.ReturnRequestId, returnReq.RefundAmount, "REF" + DateTime.UtcNow.Ticks, "SHOP");

        if (refundSuccess)
        {
            var refundPayment = new Payment
            {
                OrderId = dto.OrderId,
                ReturnRequestId = dto.ReturnRequestId,
                PaymentType = PaymentType.REFUND,
                Method = PaymentMethod.ONLINE,
                Amount = returnReq.RefundAmount,
                Status = PaymentStatus.SUCCESS,
                CreatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(refundPayment);

            returnReq.OrderDetail.ReturnStatus = ReturnStatus.REFUNDED;

            var transaction = new TransactionHistory
            {
                UserId = order.UserId,
                ReferenceType = TransactionReferenceType.REFUND,
                ReferenceId = returnReq.ReturnRequestId,
                TransactionType = TransactionType.OUT,
                Amount = returnReq.RefundAmount,
                TransactionCode = "REF" + Guid.NewGuid().ToString("N")[..10],
                Description = $"Refund for Order #{dto.OrderId}, ReturnRequest #{dto.ReturnRequestId}",
                CreatedAt = DateTime.UtcNow
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
