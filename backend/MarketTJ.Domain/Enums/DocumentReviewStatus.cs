namespace MarketTJ.Domain.Enums;

// Отдельный enum от FarmerVerificationStatus — у FarmerDocument другой набор
// значений (нет Suspended/Verified-в-смысле-VerificationStatus, только
// Pending/Approved/Rejected для отдельного документа).
public enum DocumentReviewStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
