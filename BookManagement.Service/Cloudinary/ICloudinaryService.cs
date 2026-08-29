using Microsoft.AspNetCore.Http;

namespace BookManagement.Service.Cloudinary;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
    Task<bool> DeleteImageAsync(string publicId);
}
