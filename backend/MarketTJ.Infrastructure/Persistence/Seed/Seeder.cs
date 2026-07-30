using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketTJ.Infrastructure.Persistence.Seed;

public static class Seeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var farmerProfileRepository = scope.ServiceProvider.GetRequiredService<IFarmerProfileRepository>();
        var customerProfileRepository = scope.ServiceProvider.GetRequiredService<ICustomerProfileRepository>();
        var courierProfileRepository = scope.ServiceProvider.GetRequiredService<ICourierProfileRepository>();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var orderItemRepository = scope.ServiceProvider.GetRequiredService<IOrderItemRepository>();

        var (admin, farmer, customer, courier) = await SeedUsersAsync(context, userRepository);
        var farmerProfile = await SeedFarmerProfileAsync(context, farmerProfileRepository, farmer, admin);
        await SeedCustomerProfileAsync(context, customerProfileRepository, customer);
        await SeedCourierProfileAsync(context, courierProfileRepository, courier);

        var categoryIds = await SeedCategoriesAsync(context);
        var productIds = await SeedProductsAsync(context, categoryIds);
        await SeedProductListingsAsync(context, productIds, categoryIds, farmerProfile);

        // Без этого Admin/Farmer dashboard-аналитика (выручка, заказы по месяцам,
        // рейтинг фермера) показывает одни нули — не ошибка, а просто нет заказов
        // в тестовых данных.
        await SeedOrdersAsync(context, orderRepository, orderItemRepository, customer, farmerProfile);
    }

    // Раздел 22 ТЗ: роли Admin/Farmer/Customer/Customer/Courier — по одному
    // тестовому пользователю на каждую роль из реального enum UserRole.
    // Auth/регистрация в проекте ещё не реализованы (Этап 2), устоявшегося
    // способа хеширования паролей в коде нет — используется BCrypt.Net.
    private static async Task<(User Admin, User Farmer, User Customer, User Courier)> SeedUsersAsync(
        AppDbContext context, IUserRepository userRepository)
    {
        // Раздел 22 ТЗ: email админа и пароль через переменную окружения.
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@12345!";

        var seedUsers = new[]
        {
            ("Admin Market", "admin@market.tj", "+992900000001", adminPassword, UserRole.Admin),
            ("Fermer Test", "farmer@market.tj", "+992900000002", "Farmer@12345!", UserRole.Farmer),
            ("Customer Test", "customer@market.tj", "+992900000003", "Customer@12345!", UserRole.Customer),
            ("Courier Test", "courier@market.tj", "+992900000004", "Courier@12345!", UserRole.Courier)
        };

        var result = new Dictionary<UserRole, User>();

        foreach (var (fullName, email, phone, password, role) in seedUsers)
        {
            var existing = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (existing is not null)
            {
                result[role] = existing;
                continue;
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user);
            result[role] = user;
        }

        return (result[UserRole.Admin], result[UserRole.Farmer], result[UserRole.Customer], result[UserRole.Courier]);
    }

    // Раздел 10.1: неподтверждённый фермер не может создать активное объявление —
    // тестовый профиль сразу подтверждён (Verified), иначе SeedProductListingsAsync
    // не сможет создать ни одного Active-объявления.
    private static async Task<FarmerProfile> SeedFarmerProfileAsync(
        AppDbContext context, IFarmerProfileRepository farmerProfileRepository, User farmer, User admin)
    {
        var existing = await context.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == farmer.Id);
        if (existing is not null)
            return existing;

        var profile = new FarmerProfile
        {
            UserId = farmer.Id,
            FarmName = "Фермерское хозяйство «Хосилот»",
            Region = "Хатлон",
            District = "Бохтар",
            Village = "Навобод",
            Address = "ул. Марказӣ, 12",
            Description = "Семейное хозяйство: овощи, фрукты и зелень с собственных полей.",
            VerificationStatus = FarmerVerificationStatus.Verified,
            VerifiedAt = DateTime.UtcNow,
            VerifiedByAdminId = admin.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await farmerProfileRepository.AddAsync(profile);
        return profile;
    }

    private static async Task SeedCustomerProfileAsync(
        AppDbContext context, ICustomerProfileRepository customerProfileRepository, User customer)
    {
        if (await context.CustomerProfiles.AnyAsync(c => c.UserId == customer.Id))
            return;

        await customerProfileRepository.AddAsync(new CustomerProfile
        {
            UserId = customer.Id,
            CustomerType = CustomerType.Retail,
            DefaultAddress = "г. Душанбе, ул. Рудаки, 45",
            Region = "Душанбе",
            District = "Сино",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static async Task SeedCourierProfileAsync(
        AppDbContext context, ICourierProfileRepository courierProfileRepository, User courier)
    {
        if (await context.CourierProfiles.AnyAsync(c => c.UserId == courier.Id))
            return;

        await courierProfileRepository.AddAsync(new CourierProfile
        {
            UserId = courier.Id,
            TransportType = "Автомобиль",
            VehicleNumber = "01 A 123 AA",
            Region = "Душанбе",
            District = "Сино",
            IsAvailable = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    // Раздел 22 ТЗ: точный список категорий.
    private static readonly string[] CategoryNames =
    [
        "Овощи",
        "Фрукты",
        "Зелень",
        "Сухофрукты",
        "Орехи",
        "Молочная продукция"
    ];

    private static async Task<Dictionary<string, int>> SeedCategoriesAsync(AppDbContext context)
    {
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(CategoryNames.Select(name => new Category
            {
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }));

            await context.SaveChangesAsync();
        }

        return await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
    }

    // Раздел 8.6 ТЗ прямо называет Помидор/Картофель/Лук/Яблоко/Виноград как
    // примеры Product; остальные — типичные для таджикского рынка продукты в
    // рамках тех же 6 категорий раздела 22, чтобы было из чего собрать 15-20
    // объявлений (раздел 8.6: "Примеры" — список не закрытый).
    private static readonly (string Category, string Name, string Unit)[] ProductDefs =
    [
        ("Овощи", "Помидор", "кг"),
        ("Овощи", "Картофель", "кг"),
        ("Овощи", "Лук", "кг"),
        ("Овощи", "Морковь", "кг"),
        ("Фрукты", "Яблоко", "кг"),
        ("Фрукты", "Виноград", "кг"),
        ("Фрукты", "Гранат", "кг"),
        ("Зелень", "Укроп", "кг"),
        ("Зелень", "Кинза", "кг"),
        ("Сухофрукты", "Курага", "кг"),
        ("Сухофрукты", "Изюм", "кг"),
        ("Орехи", "Грецкий орех", "кг"),
        ("Молочная продукция", "Молоко", "л"),
        ("Молочная продукция", "Курут", "кг")
    ];

    private static async Task<Dictionary<string, int>> SeedProductsAsync(AppDbContext context, Dictionary<string, int> categoryIds)
    {
        if (!await context.Products.AnyAsync())
        {
            context.Products.AddRange(ProductDefs.Select(p => new Product
            {
                CategoryId = categoryIds[p.Category],
                Name = p.Name,
                Unit = p.Unit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }));

            await context.SaveChangesAsync();
        }

        return await context.Products.ToDictionaryAsync(p => p.Name, p => p.Id);
    }

    // 16 объявлений (раздел: "15-20 реалистичных товаров"), все от тестового
    // Farmer. Region/District повторяют FarmerProfile — объявление публикуется
    // там же, где хозяйство (раздел 8.7).
    private static async Task SeedProductListingsAsync(
        AppDbContext context, Dictionary<string, int> productIds, Dictionary<string, int> categoryIds, FarmerProfile farmerProfile)
    {
        if (await context.ProductListings.AnyAsync())
            return;

        // CategoryId/Unit теперь живут прямо на ProductListing (см. миграцию
        // AddCategoryAndUnitToProductListing) — ProductId оставлен только для
        // обратной совместимости, но новые поля обязательны у всех строк.
        var categoryAndUnitByProduct = ProductDefs.ToDictionary(p => p.Name, p => (CategoryId: categoryIds[p.Category], p.Unit));

        var listings = new (string Product, string Title, decimal Retail, decimal? Wholesale, decimal? WholesaleMin, decimal Available, decimal MinOrder, string Grade)[]
        {
            ("Помидор", "Помидор грунтовый, свежий сбор", 12.50m, 10.00m, 50m, 300m, 3m, "Первый сорт"),
            ("Помидор", "Помидор тепличный розовый", 18.00m, 15.00m, 40m, 150m, 2m, "Премиум"),
            ("Картофель", "Картофель молодой", 8.00m, 6.50m, 100m, 800m, 5m, "Стандарт"),
            ("Картофель", "Картофель отборный крупный", 9.50m, 7.50m, 100m, 600m, 5m, "Первый сорт"),
            ("Лук", "Лук репчатый жёлтый", 6.00m, 4.50m, 100m, 500m, 5m, "Стандарт"),
            ("Морковь", "Морковь свежая мытая", 7.00m, 5.50m, 80m, 400m, 5m, "Первый сорт"),
            ("Яблоко", "Яблоко Семеринка", 15.00m, 12.00m, 50m, 350m, 3m, "Первый сорт"),
            ("Яблоко", "Яблоко красное сладкое", 17.50m, 14.00m, 50m, 250m, 3m, "Премиум"),
            ("Виноград", "Виноград чёрный кишмишный", 25.00m, 20.00m, 30m, 180m, 2m, "Премиум"),
            ("Гранат", "Гранат крупноплодный", 22.00m, 18.00m, 30m, 200m, 2m, "Первый сорт"),
            ("Укроп", "Укроп свежий пучками", 20.00m, null, null, 60m, 1m, "Стандарт"),
            ("Кинза", "Кинза свежая", 20.00m, null, null, 50m, 1m, "Стандарт"),
            ("Курага", "Курага вяленая без косточки", 60.00m, 50.00m, 20m, 120m, 1m, "Премиум"),
            ("Изюм", "Изюм тёмный сушёный", 45.00m, 38.00m, 20m, 100m, 1m, "Первый сорт"),
            ("Грецкий орех", "Грецкий орех очищенный", 90.00m, 75.00m, 15m, 90m, 1m, "Премиум"),
            ("Молоко", "Молоко коровье домашнее", 14.00m, null, null, 200m, 2m, "Стандарт")
        };

        var entities = listings.Select(l => new ProductListing
        {
            FarmerProfileId = farmerProfile.Id,
            ProductId = productIds[l.Product],
            CategoryId = categoryAndUnitByProduct[l.Product].CategoryId,
            Unit = categoryAndUnitByProduct[l.Product].Unit,
            Title = l.Title,
            RetailPricePerKg = l.Retail,
            WholesalePricePerKg = l.Wholesale,
            WholesaleMinimumQuantity = l.WholesaleMin,
            AvailableQuantity = l.Available,
            MinimumOrderQuantity = l.MinOrder,
            QualityGrade = l.Grade,
            Region = farmerProfile.Region,
            District = farmerProfile.District,
            Address = farmerProfile.Address,
            Status = ListingStatus.Active,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.ProductListings.AddRange(entities);
        await context.SaveChangesAsync();
    }

    // Тестовые заказы для единственных сидированных Customer/Farmer — чтобы
    // Admin/Farmer dashboard (выручка, заказы по месяцам, топ-фермеры) и
    // раздел отзывов на фронте не были пустыми "из коробки". 4 завершённых
    // заказа (с Payment + Review) разнесены по последним месяцам — для
    // графика "Фармоишҳо аз рӯи моҳҳо"; ещё 2 — в процессе (без Payment/Review).
    private static async Task SeedOrdersAsync(
        AppDbContext context, IOrderRepository orderRepository, IOrderItemRepository orderItemRepository,
        User customer, FarmerProfile farmerProfile)
    {
        if (await context.Orders.AnyAsync())
            return;

        var customerProfile = await context.CustomerProfiles.FirstAsync(c => c.UserId == customer.Id);
        var listings = await context.ProductListings
            .Where(p => p.FarmerProfileId == farmerProfile.Id)
            .OrderBy(p => p.Id)
            .Take(6)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var orderDefs = new (string Number, OrderStatus Status, int MonthsAgo, decimal DeliveryPrice, int Rating, string? ReviewComment)[]
        {
            ("ORD-1001", OrderStatus.Completed, 4, 15m, 5, "Всё свежее, привезли вовремя."),
            ("ORD-1002", OrderStatus.Completed, 3, 15m, 4, "Хорошее качество, доставка чуть задержалась."),
            ("ORD-1003", OrderStatus.Completed, 2, 20m, 5, null),
            ("ORD-1004", OrderStatus.Completed, 1, 15m, 5, "Буду заказывать ещё."),
            ("ORD-1005", OrderStatus.Accepted, 0, 15m, 0, null),
            ("ORD-1006", OrderStatus.Pending, 0, 15m, 0, null)
        };

        for (var i = 0; i < orderDefs.Length; i++)
        {
            var def = orderDefs[i];
            var items = new[]
            {
                (Listing: listings[i % listings.Count], Quantity: 2m),
                (Listing: listings[(i + 1) % listings.Count], Quantity: 3m)
            };

            var subtotal = items.Sum(x => x.Listing.RetailPricePerKg * x.Quantity);
            var createdAt = now.AddMonths(-def.MonthsAgo).AddDays(-i);

            var order = new Order
            {
                OrderNumber = def.Number,
                CustomerId = customerProfile.Id,
                FarmerId = farmerProfile.Id,
                Status = def.Status,
                DeliveryAddress = customerProfile.DefaultAddress ?? "г. Душанбе, ул. Рудаки, 45",
                Region = customerProfile.Region,
                District = customerProfile.District,
                Subtotal = subtotal,
                DeliveryPrice = def.DeliveryPrice,
                TotalAmount = subtotal + def.DeliveryPrice,
                IsDeleted = false,
                CreatedAt = createdAt,
                AcceptedAt = def.Status is OrderStatus.Accepted or OrderStatus.Completed ? createdAt.AddHours(2) : null,
                CompletedAt = def.Status == OrderStatus.Completed ? createdAt.AddDays(1) : null
            };

            await orderRepository.AddAsync(order);

            foreach (var (listing, quantity) in items)
            {
                await orderItemRepository.AddAsync(new OrderItem
                {
                    OrderId = order.Id,
                    ProductListingId = listing.Id,
                    ProductName = listing.Title,
                    UnitPrice = listing.RetailPricePerKg,
                    Quantity = quantity,
                    TotalPrice = listing.RetailPricePerKg * quantity,
                    CreatedAt = createdAt
                });
            }

            if (def.Status == OrderStatus.Completed)
            {
                context.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Completed,
                    PaidAt = order.CompletedAt,
                    CreatedAt = order.CreatedAt
                });

                context.Reviews.Add(new Review
                {
                    OrderId = order.Id,
                    CustomerId = customerProfile.Id,
                    FarmerId = farmerProfile.Id,
                    Rating = def.Rating,
                    Comment = def.ReviewComment,
                    CreatedAt = order.CompletedAt!.Value
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
