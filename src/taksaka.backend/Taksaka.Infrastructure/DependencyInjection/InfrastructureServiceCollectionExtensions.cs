using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Persistence;

namespace Taksaka.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<SerilogOptions>(configuration.GetSection(SerilogOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Taksaka")
            ?? configuration.GetSection(DatabaseOptions.SectionName)["ConnectionString"]
            ?? "Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<TaksakaDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
