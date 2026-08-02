namespace MarketTJ.Domain.Enums;

public enum FarmerDocumentType
{
    Passport = 1,
    LandDeed = 2,
    Other = 3,

    // Дополнено по явному запросу пользователя (2026-08-02) — раньше был один
    // общий тип "Passport", теперь три конкретных обязательных для добавления
    // товара документа. Старый Passport=1 оставлен как есть (не переиспользуем
    // значение) на случай уже загруженных документов этого типа.
    PassportFront = 4,
    PassportBack = 5,
    Selfie = 6
}
