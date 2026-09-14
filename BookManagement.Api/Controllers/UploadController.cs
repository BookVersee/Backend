using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Cloudinary;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

public class UploadMultipleImagesRequest
{
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    public string? Folder { get; set; } = "bookverse/books";
}

public class DeleteMultipleImagesRequest
{
    public List<string> PublicIds { get; set; } = new List<string>();
}

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[Authorize]
[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public UploadController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// Chức năng: Upload hàng loạt hình ảnh song song lên Cloudinary (Hỗ trợ 1 hoặc nhiều ảnh)
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImages([FromForm] UploadMultipleImagesRequest request)
    {
        var result = await _cloudinaryService.UploadImagesAsync(request.Files, request.Folder);
        return Ok(ApiResponse<List<ImageUploadDto>>.SuccessResponse(result, $"Đã upload thành công {result.Count} hình ảnh lên Cloudinary!"));
    }

    /// Chức năng: Xóa hàng loạt tệp hình ảnh trên Cloudinary (Hỗ trợ 1 hoặc nhiều PublicId)
    [HttpDelete("images")]
    public async Task<IActionResult> DeleteImages([FromBody] DeleteMultipleImagesRequest request)
    {
        var deletedCount = await _cloudinaryService.DeleteImagesAsync(request.PublicIds);
        return Ok(ApiResponse<object>.SuccessResponse(new { deletedCount }, $"Đã xóa thành công {deletedCount} ảnh khỏi Cloudinary."));
    }
}
