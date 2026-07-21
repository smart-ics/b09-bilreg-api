using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taksaka.Abstractions.Persistence;

namespace Taksaka.Infrastructure.Database;

public sealed class DatabaseInitializerHostedService(
    IDatabaseInitializer databaseInitializer,
    ILogger<DatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing Taksaka database schema.");
        await databaseInitializer.InitializeAsync(cancellationToken);
        logger.LogInformation("Taksaka database schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
