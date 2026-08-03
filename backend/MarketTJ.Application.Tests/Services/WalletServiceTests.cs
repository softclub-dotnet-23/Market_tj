using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Application.Validators;
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

    // Дописывает контрольную цифру по алгоритму Луна к 15-значному префиксу,
    // чтобы получить гарантированно валидный (и предсказуемый по BIN) номер
    // карты для тестов — надёжнее, чем полагаться на память "известных"
    // тестовых номеров.
    private static string ValidCardNumber(string bin)
    {
        var prefix = bin.PadRight(15, '0');
        var sum = 0;
        var alternate = true;
        for (var i = prefix.Length - 1; i >= 0; i--)
        {
            var digit = prefix[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alternate = !alternate;
        }
        var checkDigit = (10 - sum % 10) % 10;
        return prefix + checkDigit;
    }

    private static readonly string VisaNumber = ValidCardNumber("4");
    private static readonly string MastercardNumber = ValidCardNumber("5");
    private static readonly string UnionPayNumber = ValidCardNumber("62");

    // В пределах WalletValidator.MaxCardsPerUser... нет, в пределах "не более
    // чем через 15 лет" (см. WalletValidator.ValidateCreate) — 2099 было бы
    // отклонено как "некорректный срок действия", поэтому берём дату
    // динамически, а не хардкодим далёкий год.
    private static readonly int DefaultTestExpiryYear = DateTime.UtcNow.Year + 5;

    private static CreateWalletDto CreateWalletDto(string? cardNumber = null, string cvv = "123", int expiryMonth = 12, int? expiryYear = null, string bankName = "Amonatbonk") => new()
    {
        CardHolderFirstName = "Alice",
        CardHolderLastName = "Smith",
        CardNumber = cardNumber ?? VisaNumber,
        Cvv = cvv,
        ExpiryMonth = expiryMonth,
        ExpiryYear = expiryYear ?? DefaultTestExpiryYear,
        BankName = bankName
    };

    private static Wallet CreateWallet(int id = 1, int userId = 42, decimal balance = 100, int version = 0, DateTime? createdAt = null) => new()
    {
        Id = id,
        UserId = userId,
        CardHolderFirstName = "Alice",
        CardHolderLastName = "Smith",
        CardType = CardType.Visa,
        CardNumber = VisaNumber,
        Cvv = "123",
        ExpiryMonth = 12,
        ExpiryYear = 2099,
        BankName = "Amonatbonk",
        CardNumberLast4 = VisaNumber[^4..],
        Balance = balance,
        Version = version,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // === CreateAsync ===

    [Fact]
    public async Task CreateAsync_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((int?)null);

        var result = await _service.CreateAsync(CreateWalletDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CreateAsync_MissingFirstName_ReturnsValidationError()
    {
        var dto = CreateWalletDto();
        dto.CardHolderFirstName = "  ";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryAddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Theory]
    [InlineData("4111111111111112")] // Valid BIN (Visa), но контрольная цифра испорчена — не проходит Луна.
    [InlineData("1234567890123456")] // Luhn-корректный формально не проверяем — BIN не поддерживается (не 4/5/62...).
    public async Task CreateAsync_InvalidCardNumber_ReturnsValidationError(string cardNumber)
    {
        var result = await _service.CreateAsync(CreateWalletDto(cardNumber: cardNumber));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryAddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ExpiredCard_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(CreateWalletDto(expiryMonth: 1, expiryYear: 2020));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateAsync_MissingBankName_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(CreateWalletDto(bankName: "  "));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData("41")]
    [InlineData("55")]
    [InlineData("6222")]
    public async Task CreateAsync_ValidCardNumber_DetectsCardTypeFromBin(string bin)
    {
        var expectedType = bin.StartsWith("62") ? CardType.UnionPay : bin.StartsWith('4') ? CardType.Visa : CardType.Mastercard;
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync([]);
        Wallet? captured = null;
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>()))
            .Callback<Wallet>(w => captured = w)
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(CreateWalletDto(cardNumber: ValidCardNumber(bin)));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedType, captured!.CardType);
        Assert.Equal(expectedType, result.Data!.CardType);
    }

    [Fact]
    public async Task CreateAsync_AtMaxCards_ReturnsConflictWithoutWritingToRepository()
    {
        var fiveCards = Enumerable.Range(1, WalletValidator.MaxCardsPerUser).Select(i => CreateWallet(id: i)).ToList();
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync(fiveCards);

        var result = await _service.CreateAsync(CreateWalletDto(cardNumber: MastercardNumber));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _walletRepository.Verify(r => r.TryAddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_BelowMaxCards_Succeeds()
    {
        var fourCards = Enumerable.Range(1, WalletValidator.MaxCardsPerUser - 1).Select(i => CreateWallet(id: i)).ToList();
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync(fourCards);
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>())).ReturnsAsync(true);

        var result = await _service.CreateAsync(CreateWalletDto(cardNumber: MastercardNumber));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesWalletWithZeroBalanceAndOwnUserIdAndMaskedLast4()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync([]);
        Wallet? captured = null;
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>()))
            .Callback<Wallet>(w => captured = w)
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(CreateWalletDto(cardNumber: VisaNumber));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data!.Balance);
        // Владелец карты — только currentUser.UserId, а не что-то из dto
        // (в CreateWalletDto вообще нет поля UserId — подделать чужого
        // владельца через тело запроса невозможно в принципе).
        Assert.Equal(42, captured!.UserId);
        Assert.Equal(VisaNumber[^4..], captured.CardNumberLast4);
        Assert.Equal(VisaNumber, captured.CardNumber);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CvvNeverAppearsInResponseDto()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync([]);
        _walletRepository.Setup(r => r.TryAddAsync(It.IsAny<Wallet>())).ReturnsAsync(true);

        var result = await _service.CreateAsync(CreateWalletDto());

        Assert.True(result.IsSuccess);
        // GetWalletDto физически не содержит свойства Cvv/CardNumber —
        // компилируется только потому, что их там нет; это утверждение
        // документирует контракт (write-only CVV, см. GetWalletDto).
        var dtoType = result.Data!.GetType();
        Assert.Null(dtoType.GetProperty("Cvv"));
        Assert.Null(dtoType.GetProperty("CardNumber"));
    }

    // === GetMyWalletsAsync ===

    [Fact]
    public async Task GetMyWalletsAsync_ReturnsAllCardsForCurrentUserOnly()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(42)).ReturnsAsync([CreateWallet(id: 1), CreateWallet(id: 2)]);

        var result = await _service.GetMyWalletsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
        _walletRepository.Verify(r => r.GetAllByUserIdAsync(42), Times.Once);
    }

    // === TopUpAsync ===

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task TopUpAsync_NonPositiveAmount_ReturnsValidationError(decimal amount)
    {
        var result = await _service.TopUpAsync(1, new TopUpWalletDto { Amount = amount });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_WalletNotFound_ReturnsNotFound()
    {
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Wallet?)null);

        var result = await _service.TopUpAsync(1, new TopUpWalletDto { Amount = 100 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task TopUpAsync_WalletBelongsToAnotherUser_ReturnsForbidden()
    {
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateWallet(userId: 999));

        var result = await _service.TopUpAsync(1, new TopUpWalletDto { Amount = 100 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    [Fact]
    public async Task TopUpAsync_Valid_IncreasesBalanceAndRecordsTopUpTransaction()
    {
        var wallet = CreateWallet(balance: 100);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        WalletTransaction? capturedTransaction = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => capturedTransaction = t)
            .ReturnsAsync(true);

        var result = await _service.TopUpAsync(1, new TopUpWalletDto { Amount = 50 });

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
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);

        var result = await _service.TopUpAsync(1, new TopUpWalletDto { Amount = 20_000 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === GetTransactionsAsync ===

    [Fact]
    public async Task GetTransactionsAsync_WalletBelongsToAnotherUser_ReturnsForbidden()
    {
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateWallet(userId: 999));

        var result = await _service.GetTransactionsAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    // === DebitForOrderAsync ===

    [Fact]
    public async Task DebitForOrderAsync_InsufficientBalance_ReturnsValidationFailureWithoutWriting()
    {
        var wallet = CreateWallet(balance: 30);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);

        var result = await _service.DebitForOrderAsync(42, walletId: 1, amount: 100, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DebitForOrderAsync_WalletNotFound_ReturnsNotFound()
    {
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Wallet?)null);

        var result = await _service.DebitForOrderAsync(42, walletId: 1, amount: 100, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DebitForOrderAsync_WalletBelongsToAnotherUser_ReturnsForbiddenAndDoesNotDebit()
    {
        // IDOR: покупатель передал walletId чужой карты — списание должно
        // быть отклонено, а не списать деньги с чужого баланса.
        var wallet = CreateWallet(id: 1, userId: 999, balance: 500);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);

        var result = await _service.DebitForOrderAsync(customerUserId: 42, walletId: 1, amount: 100, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DebitForOrderAsync_Success_RecordsPurchaseTransactionWithNegativeAmountOnSelectedWallet()
    {
        var selectedWallet = CreateWallet(id: 2, balance: 100);
        _walletRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(selectedWallet);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.DebitForOrderAsync(42, walletId: 2, amount: 60, orderId: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionType.Purchase, captured!.Type);
        Assert.Equal(-60, captured.Amount);
        Assert.Equal(40, captured.BalanceAfter);
        Assert.Equal(7, captured.RelatedOrderId);
        Assert.Equal(2, captured.WalletId);
        // Другой кошелёк того же пользователя не должен был вообще
        // запрашиваться — списание строго с выбранной карты.
        _walletRepository.Verify(r => r.GetByIdAsync(It.Is<int>(id => id != 2)), Times.Never);
    }

    [Fact]
    public async Task DebitForOrderAsync_ConcurrentConflictOnce_RereadsFreshBalanceAndRetries()
    {
        var wallet = CreateWallet(balance: 100, version: 0);
        var walletAfterConcurrentChange = CreateWallet(balance: 70, version: 1);
        _walletRepository.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(wallet)
            .ReturnsAsync(walletAfterConcurrentChange);
        var capturedTransactions = new List<WalletTransaction>();
        var callCount = 0;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => capturedTransactions.Add(t))
            .ReturnsAsync(() => ++callCount > 1);

        var result = await _service.DebitForOrderAsync(42, walletId: 1, amount: 60, orderId: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, capturedTransactions.Last().BalanceAfter);
        _walletRepository.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DebitForOrderAsync_ConcurrentConflictExceedsMaxRetries_ReturnsConflictFailure()
    {
        var wallet = CreateWallet(balance: 100);
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(wallet);
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>())).ReturnsAsync(false);

        var result = await _service.DebitForOrderAsync(42, walletId: 1, amount: 10, orderId: 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _walletRepository.Verify(r => r.GetByIdAsync(1), Times.Exactly(5));
    }

    // === CreditFarmerForOrderAsync ===

    [Fact]
    public async Task CreditFarmerForOrderAsync_ActiveCommissionConfigured_CreditsAmountMinusCommission()
    {
        var wallet = CreateWallet(id: 1, userId: 99, balance: 0);
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(99)).ReturnsAsync([wallet]);
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
    public async Task CreditFarmerForOrderAsync_MultipleCards_CreditsOldestCard()
    {
        var oldest = CreateWallet(id: 1, userId: 99, balance: 0, createdAt: DateTime.UtcNow.AddDays(-10));
        var newest = CreateWallet(id: 2, userId: 99, balance: 0, createdAt: DateTime.UtcNow);
        // GetAllByUserIdAsync возвращает уже отсортированным по CreatedAt (см.
        // WalletRepository) — в тесте порядок задаём явно, как контракт.
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(99)).ReturnsAsync([oldest, newest]);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldest);
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, captured!.WalletId);
        _walletRepository.Verify(r => r.GetByIdAsync(2), Times.Never);
    }

    [Fact]
    public async Task CreditFarmerForOrderAsync_FarmerHasNoWallet_SkipsGracefullyWithoutFailure()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(99)).ReturnsAsync([]);

        var result = await _service.CreditFarmerForOrderAsync(99, orderSubtotal: 200, orderId: 5);

        Assert.True(result.IsSuccess);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === RefundForOrderAsync ===

    [Fact]
    public async Task RefundForOrderAsync_Success_RefundsToOriginalWalletThatWasDebited()
    {
        var wallet = CreateWallet(id: 3, balance: 40);
        var purchaseTransaction = new WalletTransaction
        {
            Id = 100,
            WalletId = 3,
            Type = WalletTransactionType.Purchase,
            Amount = -60,
            BalanceAfter = 40,
            RelatedOrderId = 7,
            CreatedAt = DateTime.UtcNow
        };
        _walletRepository.Setup(r => r.FindPurchaseTransactionForOrderAsync(7)).ReturnsAsync(purchaseTransaction);
        _walletRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(wallet);
        WalletTransaction? captured = null;
        _walletRepository.Setup(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()))
            .Callback<Wallet, WalletTransaction>((_, t) => captured = t)
            .ReturnsAsync(true);

        var result = await _service.RefundForOrderAsync(orderId: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionType.Refund, captured!.Type);
        Assert.Equal(60, captured.Amount);
        Assert.Equal(100, captured.BalanceAfter);
        Assert.Equal(3, captured.WalletId);
    }

    [Fact]
    public async Task RefundForOrderAsync_NoPurchaseTransaction_SkipsGracefullyWithoutFailure()
    {
        // Оплата наличными (CashOnDelivery) — списания через Wallet не было
        // вовсе, значит и возвращать через Wallet нечего.
        _walletRepository.Setup(r => r.FindPurchaseTransactionForOrderAsync(7)).ReturnsAsync((WalletTransaction?)null);

        var result = await _service.RefundForOrderAsync(orderId: 7);

        Assert.True(result.IsSuccess);
        _walletRepository.Verify(r => r.TryApplyTransactionAsync(It.IsAny<Wallet>(), It.IsAny<WalletTransaction>()), Times.Never);
    }

    // === GetFarmerPaymentCardAsync ===

    [Fact]
    public async Task GetFarmerPaymentCardAsync_ReturnsOnlyCardTypeLast4AndBank()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(77)).ReturnsAsync([CreateWallet(userId: 77)]);

        var result = await _service.GetFarmerPaymentCardAsync(77);

        Assert.True(result.IsSuccess);
        Assert.Equal(CardType.Visa, result.Data!.CardType);
        Assert.Equal(VisaNumber[^4..], result.Data.CardNumberLast4);
    }

    [Fact]
    public async Task GetFarmerPaymentCardAsync_NoWallet_ReturnsNullData()
    {
        _walletRepository.Setup(r => r.GetAllByUserIdAsync(77)).ReturnsAsync([]);

        var result = await _service.GetFarmerPaymentCardAsync(77);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }
}
