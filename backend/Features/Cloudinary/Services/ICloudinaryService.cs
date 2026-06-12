using Microsoft.AspNetCore.Http;

namespace backend.Features.Cloudinary.Services;

public interface ICloudinaryService
{
	Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
}