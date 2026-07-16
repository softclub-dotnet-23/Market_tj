using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class ReportedListing
{
    public int Id { get; set; }
    public int ProductListingId { get; set; }
    public int ReportedByUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Comment { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }

    // ProductListing 1 — many ReportedListing.
    public ProductListing ProductListing { get; set; } = null!;

    // User — кто пожаловался / User — Admin, рассмотревший жалобу (необязательная связь).
    public User ReportedByUser { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
}
