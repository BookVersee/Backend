using System;
using System.Threading.Tasks;
using BookManagement.Service.Cloudinary;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Folder { get; set; } = "bookverse/uploads";
}

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public UploadController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Upload hình ảnh lên Cloudinary qua multipart/form-data
    /// </summary>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
    {
        var file = request.File;
        var folder = request.Folder;

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.ErrorResponse("Vui lòng chọn file hình ảnh hợp lệ."));
        }

        // Kiểm tra định dạng ảnh cơ bản
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Array.Exists(allowedExtensions, ext => ext == extension))
        {
            return BadRequest(ApiResponse.ErrorResponse("Định dạng file không hỗ trợ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP, GIF."));
        }

        // Giới hạn dung lượng 10MB
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(ApiResponse.ErrorResponse("Dung lượng file tối đa là 10MB."));
        }

        var (url, publicId) = await _cloudinaryService.UploadImageAsync(file, string.IsNullOrWhiteSpace(folder) ? "bookverse/uploads" : folder);

        return Ok(ApiResponse.SuccessResponse(new
        {
            url = url,
            public_id = publicId,
            file_name = file.FileName,
            size = file.Length
        }, "Upload ảnh lên Cloudinary thành công!"));
    }

    /// <summary>
    /// Xóa hình ảnh trên Cloudinary theo PublicId
    /// </summary>
    /// <param name="publicId">PublicId của ảnh trên Cloudinary</param>
    [HttpDelete("image")]
    public async Task<IActionResult> DeleteImage([FromQuery] string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return BadRequest(ApiResponse.ErrorResponse("PublicId không được để trống."));
        }

        var isDeleted = await _cloudinaryService.DeleteImageAsync(publicId);
        if (!isDeleted)
        {
            return BadRequest(ApiResponse.ErrorResponse("Không tìm thấy hoặc không thể xóa ảnh trên Cloudinary."));
        }

        return Ok(ApiResponse.SuccessResponse(new { public_id = publicId }, "Xóa ảnh trên Cloudinary thành công!"));
    }
}
