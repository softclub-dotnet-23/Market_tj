using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// [Authorize] на уровне класса — userId для операций над "своими" картами
// берётся из JWT claims внутри WalletService (currentUser.UserId), а для
// операций по конкретному walletId (topup/transactions) сверяется владение
// wallet.UserId == currentUser.UserId внутри сервиса — чужую карту отсюда
// пополнить/посмотреть нельзя (см. IWalletService).
[Authorize]
[Route("api/wallet")]
public class WalletController(IWalletService walletService, IWalletPinService walletPinService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyWallets() => HandleResult(await walletService.GetMyWalletsAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletDto dto) => HandleResult(await walletService.CreateAsync(dto));

    [HttpPost("{id:int}/topup")]
    public async Task<IActionResult> TopUp(int id, [FromBody] TopUpWalletDto dto) => HandleResult(await walletService.TopUpAsync(id, dto));

    [HttpGet("{id:int}/transactions")]
    public async Task<IActionResult> GetTransactions(int id) => HandleResult(await walletService.GetTransactionsAsync(id));

    // PIN защищает ВХОД В РАЗДЕЛ "Кошелёк" на клиенте, а не сами эндпоинты
    // выше — те остаются на обычной JWT-авторизации (см. IWalletPinService).
    [HttpGet("pin/status")]
    public async Task<IActionResult> GetPinStatus() => HandleResult(await walletPinService.GetStatusAsync());

    [HttpPost("pin/set")]
    public async Task<IActionResult> SetPin([FromBody] SetWalletPinDto dto) => HandleResult(await walletPinService.SetPinAsync(dto));

    [HttpPost("pin/verify")]
    public async Task<IActionResult> VerifyPin([FromBody] VerifyWalletPinDto dto) => HandleResult(await walletPinService.VerifyPinAsync(dto));

    [HttpPut("pin/change")]
    public async Task<IActionResult> ChangePin([FromBody] ChangeWalletPinDto dto) => HandleResult(await walletPinService.ChangePinAsync(dto));

    // Публичная карточка "способ оплаты фермера" (только тип + последние 4
    // цифры + банк) — показывается любому посетителю публичного профиля
    // фермера, поэтому явно разрешаем анонимный доступ поверх [Authorize] класса,
    // как и для остального публичного каталога.
    [AllowAnonymous]
    [HttpGet("farmer/{farmerUserId:int}/payment-card")]
    public async Task<IActionResult> GetFarmerPaymentCard(int farmerUserId) => HandleResult(await walletService.GetFarmerPaymentCardAsync(farmerUserId));
}
