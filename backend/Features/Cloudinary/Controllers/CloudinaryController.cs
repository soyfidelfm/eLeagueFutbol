using backend.Features.Cloudinary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/cloudinary")]
[Authorize]
public class CloudinaryController : ControllerBase
{
	private readonly ICloudinaryService _cloudinaryService;

	public CloudinaryController(ICloudinaryService cloudinaryService)
	{
		_cloudinaryService = cloudinaryService;
	}

	[HttpPost("upload")]
	public async Task<IActionResult> Upload([FromForm] IFormFile file)
	{
		if (file is null || file.Length == 0)
			return BadRequest("File is required.");

		var result = await _cloudinaryService.UploadImageAsync(file, "clubs");

		return Ok(new
		{
			secureUrl = result.Url,
			publicId = result.PublicId
		});
	}
	[HttpPost("upload-avatar")]
	public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
	{
		if (file is null || file.Length == 0)
			return BadRequest("File is required.");

		var result = await _cloudinaryService.UploadImageAsync(file, "avatars");

		return Ok(new
		{
			secureUrl = result.Url,
			publicId = result.PublicId
		});
	}
}