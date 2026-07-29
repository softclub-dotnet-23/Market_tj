using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ReviewService(
    IReviewRepository reviewRepository,
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    ICurrentUserService currentUser,
    ILogger<ReviewService> logger) : IReviewService
{
    // Audit 2026-07-28, находка 2.2 (IDOR): только автор отзыва (Customer) или
    // Admin может его редактировать/удалять — Farmer (объект отзыва) не должен
    // иметь возможность менять/скрывать неудобный отзыв о себе.
    private async Task<bool> IsAuthorAsync(Review review)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var customerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return customerProfile is not null && customerProfile.Id == review.CustomerId;
    }

    // GetAll/GetById сознательно ОСТАЮТСЯ публичными — отзывы показываются
    // на карточке фермера всем посетителям (см. фронтенд), это не "личный"
    // ресурс. IDOR-guard нужен только на Create/Update/Delete (см. ниже).
    public async Task<Result<IEnumerable<GetReviewDto>>> GetAllAsync()
    {
        try
        {
            var reviews = await reviewRepository.GetAllAsync();
            return Result<IEnumerable<GetReviewDto>>.Ok(reviews.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка отзывов");
            return Result<IEnumerable<GetReviewDto>>.Fail("Не удалось получить список отзывов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetReviewDto?>> GetByIdAsync(int id)
    {
        try
        {
            var review = await reviewRepository.GetByIdAsync(id);
            if (review is null)
                return Result<GetReviewDto?>.Fail("Отзыв не найден", ErrorType.NotFound);

            return Result<GetReviewDto?>.Ok(ToGetDto(review));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении отзыва {Id}", id);
            return Result<GetReviewDto?>.Fail("Не удалось получить отзыв", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateReviewDto dto)
    {
        try
        {
            var validation = ReviewValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 10.6 ТЗ: отзыв доступен только после Completed.
            if (order.Status != OrderStatus.Completed)
                return Result<string>.Fail("Отзыв можно оставить только после завершения заказа", ErrorType.Validation);

            // Раздел 10.6 ТЗ: клиент может оставить отзыв только на свой заказ —
            // сверяем с РЕАЛЬНЫМ текущим пользователем (JWT), а не только с тем,
            // что dto.CustomerId внутренне совпадает с order.CustomerId (audit
            // 2026-07-28, находка 2.2 — это не одно и то же: без этой проверки
            // любой авторизованный Customer мог написать отзыв от имени
            // настоящего покупателя этого заказа, просто зная его CustomerId).
            if (!currentUser.IsAdmin())
            {
                var myCustomerProfile = currentUser.UserId is null ? null : await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (myCustomerProfile is null || myCustomerProfile.Id != order.CustomerId)
                    return Result<string>.Fail("Отзыв нельзя создать для чужого заказа", ErrorType.Forbidden);
            }

            if (order.CustomerId != dto.CustomerId)
                return Result<string>.Fail("Отзыв нельзя создать для чужого заказа", ErrorType.Unauthorized);

            if (order.FarmerId != dto.FarmerId)
                return Result<string>.Fail("FarmerId не соответствует заказу", ErrorType.Validation);

            // Раздел 10.6 ТЗ: по одному заказу можно оставить только один отзыв.
            var all = await reviewRepository.GetAllAsync();
            if (all.Any(r => r.OrderId == dto.OrderId))
                return Result<string>.Fail("Отзыв на этот заказ уже оставлен", ErrorType.Conflict);

            var review = new Review
            {
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                FarmerId = dto.FarmerId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await reviewRepository.AddAsync(review);
            return Result<string>.Ok("Отзыв создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании отзыва");
            return Result<string>.Fail("Не удалось создать отзыв", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateReviewDto dto)
    {
        try
        {
            var validation = ReviewValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var review = await reviewRepository.GetByIdAsync(id);
            if (review is null)
                return Result<string>.Fail("Отзыв не найден", ErrorType.NotFound);

            if (!await IsAuthorAsync(review))
                return Result<string>.Fail("Нет доступа к этому отзыву", ErrorType.Forbidden);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (order.CustomerId != dto.CustomerId)
                return Result<string>.Fail("Отзыв нельзя привязать к чужому заказу", ErrorType.Unauthorized);

            if (order.FarmerId != dto.FarmerId)
                return Result<string>.Fail("FarmerId не соответствует заказу", ErrorType.Validation);

            var all = await reviewRepository.GetAllAsync();
            if (all.Any(r => r.Id != id && r.OrderId == dto.OrderId))
                return Result<string>.Fail("Отзыв на этот заказ уже оставлен", ErrorType.Conflict);

            review.OrderId = dto.OrderId;
            review.CustomerId = dto.CustomerId;
            review.FarmerId = dto.FarmerId;
            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            await reviewRepository.UpdateAsync(review);
            return Result<string>.Ok("Отзыв обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении отзыва {Id}", id);
            return Result<string>.Fail("Не удалось обновить отзыв", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var review = await reviewRepository.GetByIdAsync(id);
            if (review is null)
                return Result<string>.Fail("Отзыв не найден", ErrorType.NotFound);

            if (!await IsAuthorAsync(review))
                return Result<string>.Fail("Нет доступа к этому отзыву", ErrorType.Forbidden);

            await reviewRepository.DeleteAsync(review);
            return Result<string>.Ok("Отзыв удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении отзыва {Id}", id);
            return Result<string>.Fail("Не удалось удалить отзыв", ErrorType.InternalServerError);
        }
    }

    private static GetReviewDto ToGetDto(Review review) => new()
    {
        Id = review.Id,
        OrderId = review.OrderId,
        CustomerId = review.CustomerId,
        FarmerId = review.FarmerId,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt
    };
}
