using MarketTJ.Domain.Enums;

namespace MarketTJ.WebApi.Models;

public class UploadCourierDocumentRequest
{
    public IFormFile File { get; set; } = null!;
    public int CourierProfileId { get; set; }
    public CourierDocumentType DocumentType { get; set; }
}
