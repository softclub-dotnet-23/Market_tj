using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Dto.CourierProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IDeliveryService
{
    Task<Result<IEnumerable<GetDeliveryDto>>> GetAllAsync();
    Task<Result<GetDeliveryDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateDeliveryDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateDeliveryDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Полноценное назначение/отслеживание курьера (audit 2026-08-02) —
    // отдельные методы поверх сырого CRUD выше: применяют бизнес-правила,
    // синхронизируют Order.Status, создают уведомления и запись в AuditLog.
    Task<Result<GetDeliveryDto?>> GetByOrderIdAsync(int orderId);
    Task<Result<IEnumerable<GetDeliveryDto>>> GetMyDeliveriesAsync();
    Task<Result<IEnumerable<GetAvailableCourierDto>>> GetAvailableCouriersAsync(AvailableCourierFilter filter);

    Task<Result<string>> AssignCourierAsync(int orderId, AssignCourierDto dto);
    Task<Result<string>> AssignManualCourierAsync(int orderId, AssignManualCourierDto dto);

    // Фото вместо кода подтверждения (2026-08-05) — Stream/fileName/fileLength
    // вместо DTO с IFormFile, тот же приём, что и у ICourierDocumentService.
    // UploadAsync: ASP.NET Core-специфичный IFormFile не должен протекать в
    // Application-слой, его распаковывает контроллер.
    Task<Result<string>> ConfirmManualDeliveryAsync(int deliveryId, Stream photoStream, string fileName, long fileLength);
    Task<Result<string>> UpdateAdminDetailsAsync(int deliveryId, UpdateDeliveryAdminDetailsDto dto);
    Task<Result<string>> CancelAsync(int deliveryId, CancelDeliveryDto dto);

    // Курьер отменяет СВОЮ уже назначенную доставку (Блок 2, 2026-08-08, по
    // явному запросу пользователя) — раньше отмена была доступна только
    // администратору (CancelAsync выше). Причина обязательна и валидируется
    // в IAccountBlockService (минимум несколько слов) — 3+ таких отмены за
    // 24ч блокируют аккаунт курьера на 48ч/7д (эскалация при повторе).
    Task<Result<string>> CancelByCourierAsync(int deliveryId, string reason);

    Task<Result<string>> MarkReadyForPickupAsync(int orderId);

    Task<Result<string>> AcceptAsync(int deliveryId);
    Task<Result<string>> UpdateCourierStatusAsync(int deliveryId, CourierStatusUpdateDto dto);
    Task<Result<string>> ConfirmDeliveryAsync(int deliveryId, Stream photoStream, string fileName, long fileLength);
    Task<Result<string>> ReportProblemAsync(int deliveryId, ReportDeliveryProblemDto dto);
}

public class AvailableCourierFilter
{
    // Заказ, для которого подбираются курьеры — координаты адреса доставки
    // резолвятся сервером (геокодируются лениво и кэшируются на Order, см.
    // DeliveryService.GetAvailableCouriersAsync), не передаются клиентом.
    public int OrderId { get; set; }
    public bool OnlyAvailable { get; set; }
    public string? Region { get; set; }
    public string? District { get; set; }
    public string? TransportType { get; set; }
    public decimal? MinRating { get; set; }
}
