using FluentValidation;
using Taksaka.Server.Configuration;
using Taksaka.Server.HostedServices;

namespace Taksaka.Server.DependencyInjection;

public static class ServerServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PluginLoaderOptions>(configuration.GetSection(PluginLoaderOptions.SectionName));

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddSignalR();

        services.AddCors(options =>
        {
            options.AddPolicy("TaksakaCors", policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddHostedService<EngineHostedService>();
        services.AddHostedService<PluginLoaderHostedService>();

        return services;
    }
}
