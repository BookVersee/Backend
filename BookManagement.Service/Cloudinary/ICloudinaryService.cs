using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Service.Cloudinary;

public class ImageUploadDto
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
    Task<List<ImageUploadDto>> UploadImagesAsync(List<IFormFile> files, string? folder);
    Task<bool> DeleteImageAsync(string publicId);
    Task<int> DeleteImagesAsync(List<string> publicIds);
}
