namespace MarketTJ.WebApi.Models;

// Фото вместо кода подтверждения доставки (2026-08-05) — то же место, где
// IFormFile распаковывается в Stream/fileName/fileLength перед вызовом
// IDeliveryService (см. UploadCourierDocumentRequest — тот же приём).
public class ConfirmDeliveryPhotoRequest
{
    public IFormFile Photo { get; set; } = null!;
}
