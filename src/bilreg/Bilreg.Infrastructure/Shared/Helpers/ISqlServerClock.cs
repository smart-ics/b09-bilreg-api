namespace Bilreg.Infrastructure.Shared.Helpers;

/// <summary>
/// Thin seam for SQL Server GETDATE() so TglJamProvider can be unit-tested without a live database.
/// </summary>
public interface ISqlServerClock
{
    DateTime GetDate();
}
