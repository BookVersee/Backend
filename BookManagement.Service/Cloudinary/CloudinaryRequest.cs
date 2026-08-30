using Microsoft.AspNetCore.Http;

namespace BookManagement.Service.Cloudinary
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; } = null!;
        public string? FolderName { get; set; }
    }
}
