using FluentValidation;

namespace Taksaka.Server.DependencyInjection;

public static class ServerServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddSignalR();

        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy("TaksakaCors", policy =>
            {
                policy
                    .WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
