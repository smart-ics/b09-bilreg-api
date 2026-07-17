using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions.Persistence;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Database;
using Taksaka.Infrastructure.Persistence.Repositories;

namespace Taksaka.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure(options => ConfigureDatabaseOptions(options, configuration));

        var provider = ResolveProvider(configuration);
        if (provider == DatabaseProvider.SqlServer)
        {
            services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();
        }
        else
        {
            services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
        }

        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IQueueRepository, QueueRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<IExecutionHistoryRepository, ExecutionHistoryRepository>();
        services.AddSingleton<IAlertRepository, AlertRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddHostedService<DatabaseInitializerHostedService>();

        services.Configure<SerilogOptions>(configuration.GetSection(SerilogOptions.SectionName));

        return services;
    }

    private static void ConfigureDatabaseOptions(DatabaseOptions options, IConfiguration configuration)
    {
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        var providerText = configuration[$"{DatabaseOptions.SectionName}:Provider"];
        if (!string.IsNullOrWhiteSpace(providerText))
        {
            options.Provider = providerText.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                ? DatabaseProvider.SqlServer
                : DatabaseProvider.Sqlite;
        }

        var legacyConnection = configuration.GetConnectionString("Taksaka");
        if (string.IsNullOrWhiteSpace(providerText) && !string.IsNullOrWhiteSpace(legacyConnection))
        {
            options.Provider = DatabaseProvider.SqlServer;
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            options.ConnectionString = !string.IsNullOrWhiteSpace(legacyConnection)
                ? legacyConnection
                : options.Provider == DatabaseProvider.SqlServer
                    ? "Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True"
                    : "Data Source=taksaka.db";
        }
    }

    private static DatabaseProvider ResolveProvider(IConfiguration configuration)
    {
        var options = new DatabaseOptions();
        ConfigureDatabaseOptions(options, configuration);
        return options.Provider;
    }
}
