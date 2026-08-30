using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BookManagement.Service.Cloudinary;

/// Vị trí: Infrastructure Service - Tích hợp dịch vụ đám mây Cloudinary để lưu trữ và quản lý tệp hình ảnh.
public class CloudinaryService : ICloudinaryService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> config)
    {
        var acc = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );
        _cloudinary = new CloudinaryDotNet.Cloudinary(acc);
        _cloudinary.Api.Secure = true;
    }

    /// Chức năng: Upload tệp hình ảnh lên Cloudinary CDN
    public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File không hợp lệ hoặc rỗng.", nameof(file));
        }

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Lỗi upload ảnh lên Cloudinary: {uploadResult.Error.Message}");
        }

        var secureUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;
        return (secureUrl, uploadResult.PublicId);
    }

    /// Chức năng: Xóa tệp hình ảnh khỏi Cloudinary CDN theo PublicId
    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var deletionParams = new DeletionParams(publicId)
        {
            Invalidate = true
        };
        var result = await _cloudinary.DestroyAsync(deletionParams);

        return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase);
    }
}
