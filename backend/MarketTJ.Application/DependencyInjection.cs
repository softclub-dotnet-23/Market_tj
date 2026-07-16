using Microsoft.Extensions.DependencyInjection;

namespace MarketTJ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Пока пусто — сервисы/валидаторы Application-слоя ещё не созданы
        // (Этап 2+ раздела 23). Метод оставлен для единообразного вызова
        // из Program.cs, как и AddInfrastructureServices.

        return services;
    }
}
