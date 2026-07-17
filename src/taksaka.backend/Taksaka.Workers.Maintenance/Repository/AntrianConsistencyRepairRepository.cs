using Dapper;
using Microsoft.Data.SqlClient;
using Taksaka.Workers.Maintenance.Configuration;
using Taksaka.Workers.Maintenance.Models;
using Taksaka.Workers.Maintenance.Queries;

namespace Taksaka.Workers.Maintenance.Repository;

internal sealed class AntrianConsistencyRepairRepository(AntrianConsistencyRepairOptions options)
    : IAntrianConsistencyRepairRepository
{
    public async Task<IReadOnlyList<AntrianConsistencyItem>> FindInconsistentAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        connection.Open();

        var rows = await connection.QueryAsync<AntrianConsistencyItem>(new CommandDefinition(
            AntrianConsistencyRepairQueries.FindInconsistent,
            new { BatchSize = batchSize },
            cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<int> RepairAsync(
        string antrianId,
        int noUrut,
        string registrationId,
        CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        connection.Open();

        return await connection.ExecuteAsync(new CommandDefinition(
            AntrianConsistencyRepairQueries.Repair,
            new
            {
                AntrianId = antrianId,
                NoUrut = noUrut,
                RegistrationId = registrationId
            },
            cancellationToken: cancellationToken));
    }

    private SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("AntrianConsistencyRepair connectionString is required.");
        }

        return new SqlConnection(options.ConnectionString);
    }
}
