using System;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Services;
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

    [HttpPost]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto)
    {
        var result = await _deliveryService.CreateDeliveryAsync(dto);
        return StatusCode(201, ApiResponse.SuccessResponse(result, "Delivery created successfully"));
    }

    [HttpGet("manifest/{delivery_id}")]
    [Authorize(Roles = "SHIPPER,ADMIN,SHOP")]
    public async Task<IActionResult> GetDeliveryManifestDetail([FromRoute(Name = "delivery_id")] Guid deliveryId)
    {
        var result = await _deliveryService.GetDeliveryManifestDetailAsync(deliveryId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPut("{delivery_id}/status")]
    [Authorize(Roles = "SHIPPER,ADMIN")]
    public async Task<IActionResult> UpdateDeliveryStatus([FromRoute(Name = "delivery_id")] Guid deliveryId, [FromBody] UpdateDeliveryStatusDto dto)
    {
        await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Delivery status updated successfully"));
    }
}
