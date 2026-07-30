namespace MarketTJ.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    // Name — русское название (каноническое, уникальное). NameTj/NameEn —
    // переводы для таджикского/английского UI (см. Frontend/src/data/categories.ts),
    // добавлены отдельными nullable-полями, а не JSON-блобом, чтобы старые
    // строки (засеянные до ввода переводов) продолжали работать без миграции
    // данных — просто откатываются на Name как фолбэк.
    public string Name { get; set; } = null!;
    public string? NameTj { get; set; }
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Category 1 — many Product / ProductListing / Commission (0..1 переопределение ставки).
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ProductListing> ProductListings { get; set; } = new List<ProductListing>();
    public ICollection<Commission> Commissions { get; set; } = new List<Commission>();
}
