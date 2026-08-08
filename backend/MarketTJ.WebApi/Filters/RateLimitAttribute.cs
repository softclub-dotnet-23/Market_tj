using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketTJ.WebApi.Filters;

// Блок 3 (2026-08-08, по явному запросу пользователя) — общий, переиспользуемый
// anti-spam механизм: НЕ жёстко привязан к одной кнопке/эндпоинту, а
// навешивается декоратором на любой "чувствительный" метод контроллера
// (см. использования — ChatMessageController.Create, CartItemController.Create,
// DeliveryController.Accept/CourierCancel/UpdateCourierStatus, OrderController.Update).
// Вся счётная/банящая логика — в IRateLimitService (Application-слой,
// протестирован юнит-тестами), этот атрибут только резолвит текущего
// пользователя из HttpContext и маппит Result в HTTP-ответ, как и
// ApiControllerBase.HandleResult для обычных экшенов.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute(int maxRequests = 15, int windowSeconds = 60) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        // Гостевые (неаутентифицированные) запросы этим механизмом не
        // ограничиваются — банить нечего, у гостя нет аккаунта; сами
        // gость-доступные эндпоинты либо защищены [Authorize] выше по стеку,
        // либо не считаются достаточно "чувствительными" для этого атрибута.
        if (currentUser.UserId is null)
        {
            await next();
            return;
        }

        var rateLimitService = context.HttpContext.RequestServices.GetRequiredService<IRateLimitService>();
        var endpointKey = $"{context.ActionDescriptor.RouteValues["controller"]}.{context.ActionDescriptor.RouteValues["action"]}";

        var result = await rateLimitService.CheckAsync(
            currentUser.UserId.Value, currentUser.Role ?? "Unknown", endpointKey, maxRequests, TimeSpan.FromSeconds(windowSeconds));

        if (!result.IsSuccess)
        {
            context.Result = new ObjectResult(new
            {
                isSuccess = false,
                message = result.Error,
                errors = new[] { result.Error }
            })
            { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await next();
    }
}
