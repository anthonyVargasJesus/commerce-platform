using System.Reflection;
using FluentValidation;
using Orders.Application.Common.Behaviours;
using Orders.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddScoped<IOrderAccessPolicy, OrderAccessPolicy>();

        return services;
    }
}
