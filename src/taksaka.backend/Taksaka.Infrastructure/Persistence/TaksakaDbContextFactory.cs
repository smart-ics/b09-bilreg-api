using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Taksaka.Infrastructure.Persistence;

public sealed class TaksakaDbContextFactory : IDesignTimeDbContextFactory<TaksakaDbContext>
{
    public TaksakaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Taksaka")
            ?? "Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<TaksakaDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TaksakaDbContext(optionsBuilder.Options);
    }
}
