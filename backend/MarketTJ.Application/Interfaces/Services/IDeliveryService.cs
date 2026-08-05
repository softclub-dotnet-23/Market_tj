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
    Task<Result<string>> ConfirmManualDeliveryAsync(int deliveryId, ConfirmDeliveryDto dto);
    Task<Result<string>> UpdateAdminDetailsAsync(int deliveryId, UpdateDeliveryAdminDetailsDto dto);
    Task<Result<string>> CancelAsync(int deliveryId, CancelDeliveryDto dto);

    Task<Result<string>> MarkReadyForPickupAsync(int orderId);

    Task<Result<string>> AcceptAsync(int deliveryId);
    Task<Result<string>> UpdateCourierStatusAsync(int deliveryId, CourierStatusUpdateDto dto);
    Task<Result<string>> ConfirmDeliveryAsync(int deliveryId, ConfirmDeliveryDto dto);
    Task<Result<string>> ReportProblemAsync(int deliveryId, ReportDeliveryProblemDto dto);
}

public class AvailableCourierFilter
{
    public bool OnlyAvailable { get; set; }
    public string? Region { get; set; }
    public string? District { get; set; }
    public string? TransportType { get; set; }
    public decimal? MinRating { get; set; }
}
