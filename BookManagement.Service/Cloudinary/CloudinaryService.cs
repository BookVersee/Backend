using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BookManagement.Service.Cloudinary;

/// Vị trí: Infrastructure Service - Tích hợp dịch vụ đám mây Cloudinary để lưu trữ và quản lý tệp hình ảnh.
public class CloudinaryService : ICloudinaryService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxBatchUploadLimit = 10;

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
        ValidateFile(file);

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

    /// Chức năng: Upload hàng loạt tệp hình ảnh song song lên Cloudinary CDN
    public async Task<List<ImageUploadDto>> UploadImagesAsync(List<IFormFile> files, string? folder)
    {
        if (files == null || files.Count == 0)
        {
            throw new ArgumentException("Vui lòng chọn ít nhất 1 file hình ảnh để upload.");
        }

        if (files.Count > MaxBatchUploadLimit)
        {
            throw new ArgumentException($"Mỗi lần upload tối đa {MaxBatchUploadLimit} ảnh.");
        }

        foreach (var file in files)
        {
            ValidateFile(file);
        }

        var targetFolder = string.IsNullOrWhiteSpace(folder) ? "bookverse/books" : folder.Trim();

        var uploadTasks = files.Select(async file =>
        {
            var (url, publicId) = await UploadImageAsync(file, targetFolder);
            return new ImageUploadDto
            {
                Url = url,
                PublicId = publicId
            };
        });

        var results = await Task.WhenAll(uploadTasks);
        return results.ToList();
    }

    /// Chức năng: Xóa tệp hình ảnh khỏi Cloudinary CDN theo PublicId
    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var deletionParams = new DeletionParams(publicId.Trim())
        {
            Invalidate = true
        };
        var result = await _cloudinary.DestroyAsync(deletionParams);

        return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase);
    }

    /// Chức năng: Xóa hàng loạt hình ảnh khỏi Cloudinary CDN
    public async Task<int> DeleteImagesAsync(List<string> publicIds)
    {
        if (publicIds == null || publicIds.Count == 0)
        {
            throw new ArgumentException("Danh sách PublicIds không được để trống.");
        }

        var deleteTasks = publicIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(DeleteImageAsync);

        var results = await Task.WhenAll(deleteTasks);
        return results.Count(r => r);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File không hợp lệ hoặc rỗng.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Array.Exists(AllowedExtensions, e => e == ext))
        {
            throw new ArgumentException($"File '{file.FileName}' có định dạng không hỗ trợ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP, GIF.");
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            throw new ArgumentException($"File '{file.FileName}' vượt quá dung lượng tối đa 10MB.");
        }
    }
}
