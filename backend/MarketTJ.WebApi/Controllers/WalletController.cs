using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// [Authorize] на уровне класса — userId для всех операций над "своим"
// кошельком берётся из JWT claims внутри WalletService (currentUser.UserId),
// никогда из тела запроса или маршрута, поэтому чужой кошелёк отсюда
// пополнить/посмотреть нельзя в принципе (нет параметра, который бы это
// позволял) — см. IWalletService.
[Authorize]
[Route("api/wallet")]
public class WalletController(IWalletService walletService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyWallet() => HandleResult(await walletService.GetMyWalletAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletDto dto) => HandleResult(await walletService.CreateAsync(dto));

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto) => HandleResult(await walletService.TopUpAsync(dto));

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions() => HandleResult(await walletService.GetMyTransactionsAsync());

    // Публичная карточка "способ оплаты фермера" (только тип + последние 4
    // цифры) — показывается любому посетителю публичного профиля фермера,
    // поэтому явно разрешаем анонимный доступ поверх [Authorize] класса,
    // как и для остального публичного каталога.
    [AllowAnonymous]
    [HttpGet("farmer/{farmerUserId:int}/payment-card")]
    public async Task<IActionResult> GetFarmerPaymentCard(int farmerUserId) => HandleResult(await walletService.GetFarmerPaymentCardAsync(farmerUserId));
}
