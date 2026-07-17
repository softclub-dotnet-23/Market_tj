using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketTJ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AddHttpClient<TInterface, TImplementation> сам регистрирует
        // IAiAssistantService со scoped-совместимым временем жизни и внедряет
        // сконфигурированный HttpClient в конструктор.
        services.AddHttpClient<IAiAssistantService, AiAssistantService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFarmerProfileService, FarmerProfileService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();

        return services;
    }
}
