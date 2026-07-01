using Microsoft.EntityFrameworkCore;

namespace Taksaka.Infrastructure.Persistence;

public sealed class TaksakaDbContext(DbContextOptions<TaksakaDbContext> options) : DbContext(options)
{
}
