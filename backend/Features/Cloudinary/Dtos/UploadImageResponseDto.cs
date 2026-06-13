namespace backend.Features.Cloudinary.Dtos;

public class UploadImageResponseDto
{
	public string Url { get; set; } = string.Empty;
	public string PublicId { get; set; } = string.Empty;
}