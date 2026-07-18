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
    ILogger<ReviewService> logger) : IReviewService
{
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

            // Раздел 10.6 ТЗ: клиент может оставить отзыв только на свой заказ.
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
