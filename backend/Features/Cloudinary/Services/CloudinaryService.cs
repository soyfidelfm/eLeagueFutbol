using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace backend.Features.Cloudinary.Services;

public class CloudinaryService : ICloudinaryService
{
	private readonly CloudinaryDotNet.Cloudinary _cloudinary;

	public CloudinaryService(IOptions<CloudinarySettings> options)
	{
		var settings = options.Value;

		var account = new Account(
			settings.CloudName,
			settings.ApiKey,
			settings.ApiSecret
		);

		_cloudinary = new CloudinaryDotNet.Cloudinary(account);
	}

	public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
	{
		using var stream = file.OpenReadStream();

		var uploadParams = new ImageUploadParams
		{
			File = new FileDescription(file.FileName, stream),
			Folder = folder
		};

		var result = await _cloudinary.UploadAsync(uploadParams);

		return (result.SecureUrl.ToString(), result.PublicId);
	}
}