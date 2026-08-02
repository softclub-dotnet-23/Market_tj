using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepository = new();
    private readonly Mock<ICommissionRepository> _commissionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<WalletService>> _logger = new();
    private readonly WalletService _service;

    public WalletServiceTests()
    {
        _service = new WalletService(_walletRepository.Object, _commissionRepository.Object, _currentUser.Object, _logger.Object);
        _currentUser.Setup(c => c.UserId).Returns(42);
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Wallet CreateWallet(int id = 1, int userId = 42, decimal balance = 100, int version = 0) => new()
    {
        Id = id,
        UserId = userId,
        CardHolderFirstName = "Alice",
        CardHolderLastName = "Smith",
        CardType = CardType.Visa,
        CardNumberLast4 = "1234",
        Balance = balance,
        Version = version,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // === CreateAsync ===

    [Fact]
    public async Task CreateAsync_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((int?)null);

        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "A", CardHolderLastName = "B", CardType = CardType.Visa });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CreateAsync_MissingFirstName_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "  ", CardHolderLastName = "Smith", CardType = CardType.Visa });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryAddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidCardType_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "A", CardHolderLastName = "B", CardType = (CardType)999 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateAsync_UserAlreadyHasWallet_ReturnsConflictWithoutWritingToRepository()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(CreateWallet());

        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "A", CardHolderLastName = "B", CardType = CardType.Visa });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _walletRepository.Verify(r => r.TryAddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RaceConditionOnUniqueIndex_ReturnsConflict()
    {
        // Гонка: два параллельных запроса на создание карты одним и тем же
        // пользователем — предварительная проверка GetByUserIdAsync ничего
        // не находит у обоих, но unique index в БД пропускает только первую
        // запись; TryAddAsync честно возвращает false для второй.
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((Wallet?)null);
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>())).ReturnsAsync(false);

        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "A", CardHolderLastName = "B", CardType = CardType.Visa });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesWalletWithZeroBalanceAndOwnUserId()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((Wallet?)null);
        Wallet? captured = null;
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>()))
            .Callback<Wallet>(w => captured = w)
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(new CreateWalletDto { CardHolderFirstName = "  Alice ", CardHolderLastName = " Smith ", CardType = CardType.Mastercard });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data!.Balance);
        Assert.Equal("Alice", captured!.CardHolderFirstName);
        Assert.Equal("Smith", captured.CardHolderLastName);
        // Владелец карты — только currentUser.UserId, а не что-то из dto
        // (в CreateWalletDto вообще нет поля UserId — подделать чужого
        // владельца через тело запроса невозможно в принципе).
        Assert.Equal(42, captured.UserId);
        Assert.Equal(4, captured.CardNumberLast4.Length);
        Assert.True(captured.CardNumberLast4.All(char.IsDigit));
    }

    // === TopUpAsync ===

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task TopUpAsync_NonPositiveAmount_ReturnsValidationError(decimal amount)
    {
        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = amount });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_MoreThanTwoDecimalPlaces_ReturnsValidationError()
    {
        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = 1000.999m });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_ExceedsMaxTopUpAmount_ReturnsValidationError()
    {
        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = 50_000.01m });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_NoWallet_ReturnsNotFound()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((Wallet?)null);

        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = 100 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_Valid_IncreasesBalanceAndRecordsTopUpTransaction()
    {
        var wallet = CreateWallet(balance: 100);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        WalletTransaction? capturedTransaction = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => capturedTransaction = t)
            .ReturnsAsync(true);

        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = 50 });

        Assert.True(result.IsSuccess);
        Assert.Equal(150, result.Data!.Balance);
        Assert.Equal(WalletTransactionType.TopUp, capturedTransaction!.Type);
        Assert.Equal(50, capturedTransaction.Amount);
        Assert.Equal(150, capturedTransaction.BalanceAfter);
    }

    [Fact]
    public async Task TopUpAsync_WouldExceedMaxBalance_ReturnsValidationError()
    {
        var wallet = CreateWallet(balance: 999_990);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);

        var result = await _service.TopUpAsync(new TopUpWalletDto { Amount = 20_000 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === DebitForOrderAsync ===

    [Fact]
    public async Task DebitForOrderAsync_InsufficientBalance_ReturnsValidationFailureWithoutWriting()
    {
        var wallet = CreateWallet(balance: 30);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);

        var result = await _service.DebitForOrderAsync(42, 100, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DebitForOrderAsync_NoWallet_ReturnsValidationFailure()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((Wallet?)null);

        var result = await _service.DebitForOrderAsync(42, 100, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task DebitForOrderAsync_Success_RecordsPurchaseTransactionWithNegativeAmount()
    {
        var wallet = CreateWallet(balance: 100);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.DebitForOrderAsync(42, 60, orderId: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionType.Purchase, captured!.Type);
        Assert.Equal(-60, captured.Amount);
        Assert.Equal(40, captured.BalanceAfter);
        Assert.Equal(7, captured.RelatedOrderId);
    }

    [Fact]
    public async Task DebitForOrderAsync_ConcurrentConflictOnce_RereadsFreshBalanceAndRetries()
    {
        // Два одновременных списания с одного кошелька: первая попытка
        // конфликтует (кто-то другой успел изменить баланс между чтением и
        // записью), вторая — с уже свежим балансом — проходит.
        var wallet = CreateWallet(balance: 100, version: 0);
        var walletAfterConcurrentChange = CreateWallet(balance: 70, version: 1);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(wallet)
            .ReturnsAsync(walletAfterConcurrentChange);
        var capturedTransactions = new List<WalletTransaction>();
        var callCount = 0;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => capturedTransactions.Add(t))
            .ReturnsAsync(() => ++callCount > 1);

        var result = await _service.DebitForOrderAsync(42, 60, orderId: 7);

        Assert.True(result.IsSuccess);
        // Второй раз баланс списывается от уже АКТУАЛЬНОГО (70), а не от
        // устаревшего первоначально прочитанного (100) — 70 - 60 = 10, не
        // "потерянное" 40. Именно это отличает корректную защиту от гонки
        // от простого retry без перечитывания состояния.
        Assert.Equal(10, capturedTransactions.Last().BalanceAfter);
        _walletRepository.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DebitForOrderAsync_ConcurrentConflictExceedsMaxRetries_ReturnsConflictFailure()
    {
        var wallet = CreateWallet(balance: 100);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>())).ReturnsAsync(false);

        var result = await _service.DebitForOrderAsync(42, 10, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _walletRepository.Verify(r => r.GetByIdAsync(1), Times.Exactly(5));
    }

    // === CreditFarmerForOrderAsync ===

    [Fact]
    public async Task CreditFarmerForOrderAsync_ActiveCommissionConfigured_CreditsAmountMinusCommission()
    {
        var wallet = CreateWallet(userId: 99, balance: 0);
        _walletRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Commission { Id = 1, CategoryId = null, Percentage = 10, EffectiveFrom = DateTime.UtcNow.AddDays(-1), EffectiveTo = null, CreatedAt = DateTime.UtcNow }
        ]);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        // 200 - 10% = 180.
        Assert.Equal(180, captured!.Amount);
        Assert.Equal(WalletTransactionType.FarmerCredit, captured.Type);
    }

    [Fact]
    public async Task CreditFarmerForOrderAsync_NoCommissionConfigured_CreditsFullSubtotal()
    {
        var wallet = CreateWallet(userId: 99, balance: 0);
        _walletRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, captured!.Amount);
    }

    [Fact]
    public async Task CreditFarmerForOrderAsync_ExpiredCommission_IsIgnored()
    {
        var wallet = CreateWallet(userId: 99, balance: 0);
        _walletRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Commission { Id = 1, CategoryId = null, Percentage = 15, EffectiveFrom = DateTime.UtcNow.AddDays(-30), EffectiveTo = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow }
        ]);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, captured!.Amount);
    }

    [Fact]
    public async Task CreditFarmerForOrderAsync_FarmerHasNoWallet_SkipsGracefullyWithoutFailure()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync((Wallet?)null);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === RefundForOrderAsync ===

    [Fact]
    public async Task RefundForOrderAsync_Success_IncreasesBalanceWithRefundTransaction()
    {
        var wallet = CreateWallet(balance: 40);
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.RefundForOrderAsync(42, 60, orderId: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionType.Refund, captured!.Type);
        Assert.Equal(60, captured.Amount);
        Assert.Equal(100, captured.BalanceAfter);
    }

    [Fact]
    public async Task RefundForOrderAsync_CustomerHasNoWallet_SkipsGracefullyWithoutFailure()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((Wallet?)null);

        var result = await _service.RefundForOrderAsync(42, 60, orderId: 7);

        Assert.True(result.IsSuccess);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === Ownership: только currentUser, никогда чужой id ===

    [Fact]
    public async Task GetMyWalletAsync_UsesOnlyCurrentUserId()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(CreateWallet());

        var result = await _service.GetMyWalletAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data!.UserId);
        _walletRepository.Verify(r => r.GetByUserIdAsync(42), Times.Once);
    }

    [Fact]
    public async Task GetFarmerPaymentCardAsync_ReturnsOnlyCardTypeAndLast4()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(77)).ReturnsAsync(CreateWallet(userId: 77));

        var result = await _service.GetFarmerPaymentCardAsync(77);

        Assert.True(result.IsSuccess);
        Assert.Equal(CardType.Visa, result.Data!.CardType);
        Assert.Equal("1234", result.Data.CardNumberLast4);
    }

    [Fact]
    public async Task GetFarmerPaymentCardAsync_NoWallet_ReturnsNullData()
    {
        _walletRepository.Setup(r => r.GetByUserIdAsync(77)).ReturnsAsync((Wallet?)null);

        var result = await _service.GetFarmerPaymentCardAsync(77);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }
}
