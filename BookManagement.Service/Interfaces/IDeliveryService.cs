using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos.Delivery;
using BookManagement.Service.Models;

namespace BookManagement.Service.Interfaces
{
    public interface IDeliveryService
    {
        Task<ApiResponse<DeliveryDto>> GetDeliveryByOrderIdAsync(Guid orderId);
        Task<ApiResponse<DeliveryDto>> CreateDeliveryAsync(CreateDeliveryRequestDto dto);
        Task<ApiResponse<DeliveryDto>> UpdateDeliveryStatusAsync(Guid deliveryId, DeliveryStatus status);
        Task<ApiResponse<bool>> HandleGhnWebhookAsync(GhnWebhookDto dto);
        Task<ApiResponse<IEnumerable<DeliveryDto>>> GetAssignedDeliveriesAsync(Guid delivererId);
    }
}
