using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Services;
using BookStore.BE2.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;

    public DeliveryController(DeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpPost("create")]
    [Authorize(Roles = "DELIVER,SHOP")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto)
    {
        var result = await _deliveryService.CreateDeliveryAsync(dto);
        return StatusCode(201, result);
    }

    [HttpPut("{delivery_id}")]
    [Authorize(Roles = "DELIVER,SHOP")]
    public async Task<IActionResult> UpdateDelivery([FromRoute(Name = "delivery_id")] int deliveryId, [FromBody] UpdateDeliveryDto dto)
    {
        var result = await _deliveryService.UpdateDeliveryAsync(deliveryId, dto);
        return Ok(result);
    }

    [HttpGet("orders")]
    [Authorize(Roles = "DELIVER")]
    public async Task<IActionResult> GetDeliveryOrders(
        [FromQuery] DeliveryStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _deliveryService.GetDeliveryOrdersAsync(status, pageIndex, pageSize);
        return Ok(result);
    }

    [HttpGet("{delivery_id}/detail")]
    [Authorize(Roles = "DELIVER")]
    public async Task<IActionResult> GetDeliveryDetail([FromRoute(Name = "delivery_id")] int deliveryId)
    {
        var result = await _deliveryService.GetDeliveryDetailAsync(deliveryId);
        return Ok(result);
    }

    [HttpPatch("{delivery_id}/status")]
    [Authorize(Roles = "DELIVER")]
    public async Task<IActionResult> UpdateDeliveryStatus([FromRoute(Name = "delivery_id")] int deliveryId, [FromBody] UpdateDeliveryStatusDto dto)
    {
        await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, dto);
        return Ok(new { message = "Delivery status synchronized" });
    }
}
