using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Common;

// Общий IDOR-guard (audit 2026-07-28, находка 2.2) — переиспользуется во всех
// сервисах с "личными" ресурсами вместо дублирования одной и той же проверки
// по 21 разу. Admin видит/меняет всё; остальные роли — только то, что
// принадлежит им самим. ownerId — либо User.Id напрямую (Notification.UserId,
// ChatMessage.SenderId и т.п.), либо Id профиля-владельца (CustomerProfile.Id/
// FarmerProfile.Id для CartItem/Order/Review и т.п.) — конкретное значение
// резолвится в каждом сервисе исходя из формы его FK, сам guard об этом не знает.
public static class CurrentUserAuthorizationExtensions
{
    public static bool IsAdmin(this ICurrentUserService currentUser)
        => currentUser.Role == nameof(UserRole.Admin);

    public static bool CanAccess(this ICurrentUserService currentUser, int ownerId)
        => currentUser.IsAdmin() || currentUser.UserId == ownerId;

    // Перегрузка для ресурсов без владельца-User (SupportTicket от гостя —
    // UserId == null): достучаться до них может только Admin, т.к. у самого
    // гостя нет сессии/аккаунта, которому можно было бы "принадлежать".
    public static bool CanAccess(this ICurrentUserService currentUser, int? ownerId)
        => currentUser.IsAdmin() || (ownerId is not null && currentUser.UserId == ownerId);
}
