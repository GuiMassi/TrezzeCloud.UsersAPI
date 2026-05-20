using MassTransit;

namespace TrezzeCloud.Users.Api.Configurations;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(config =>
        {
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMq:Host"]!,
                    "/",
                    host =>
                    {
                        host.Username(configuration["RabbitMq:Username"]!);
                        host.Password(configuration["RabbitMq:Password"]!);
                    });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}