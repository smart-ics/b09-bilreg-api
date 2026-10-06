using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using MassTransit;

namespace Bilreg.Api.Configurations;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqOption = configuration.GetSection(RabbitMqOption.SectionName);
        var server = string.IsNullOrWhiteSpace(rabbitMqOption["Server"])
            ? "localhost"
            : rabbitMqOption["Server"]!;
        var userName = rabbitMqOption["UserName"] ?? string.Empty;
        var password = rabbitMqOption["Password"] ?? string.Empty;

        services.Configure<RabbitMqOption>(rabbitMqOption);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, config) =>
            {
                config.Host(server, "/", h =>
                {
                    h.Username(userName);
                    h.Password(password);
                });
            });
        });

        services.AddScoped<IAdmisiEventPublisher, AdmisiEventPublisher>();

        return services;
    }
}
