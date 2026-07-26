namespace MarketTJ.WebApi.Models;

// IFormFile — ASP.NET Core-specific тип, поэтому этот request-класс не может
// жить в Application/Dto вместе с остальными DTO (см. IFileStorageService).
public class UploadAvatarRequest
{
    public IFormFile File { get; set; } = null!;
}
