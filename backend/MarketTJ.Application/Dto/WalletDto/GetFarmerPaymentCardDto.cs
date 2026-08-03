using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.WalletDto;

// Публичный "снимок" карты фермера для его публичного профиля — намеренно
// НЕ включает Balance/CardHolderName/UserId, только то, что безопасно
// показать любому посетителю сайта (см. запрос: "карточка «Способ оплаты
// фермера» с замаскированным номером... без полного номера"). Показывается
// самая старая карта фермера (см. WalletService.GetFarmerPaymentCardAsync) —
// намеренное упрощение, т.к. отдельного выбора "карты для выплат" в этой
// версии нет.
public class GetFarmerPaymentCardDto
{
    public CardType CardType { get; set; }
    public string CardNumberLast4 { get; set; } = null!;
    public string BankName { get; set; } = null!;
}
