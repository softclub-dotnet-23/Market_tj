namespace MarketTJ.Domain.Entities;

// Код подтверждения email при регистрации (не привязан к User — на этом
// этапе аккаунта ещё не существует, ключ — сам адрес). Код хранится
// хэшированным (как и PasswordHash у User), не открытым текстом — чтобы
// утечка базы не давала мгновенно рабочие коды.
public class EmailVerificationCode
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
