using Dapper;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Infrastructure.Persistence.Mapping;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class ScheduleRepository(IDbConnectionFactory connectionFactory) : IScheduleRepository
{
    public async Task InsertAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(new CommandDefinition(
            ScheduleQueries.Insert,
            new
            {
                schedule.Id,
                schedule.Name,
                schedule.CronExpression,
                schedule.WorkerName,
                schedule.PayloadTemplate,
                IsEnabled = schedule.IsEnabled ? 1 : 0,
                schedule.LastRunAt,
                schedule.NextRunAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Schedule>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var rows = await connection.QueryAsync<ScheduleRow>(new CommandDefinition(
            ScheduleQueries.SelectEnabled,
            new { IsEnabled = 1 },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToList();
    }

    public async Task UpdateRunTimesAsync(
        Guid scheduleId,
        DateTimeOffset lastRunAt,
        DateTimeOffset nextRunAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(new CommandDefinition(
            ScheduleQueries.UpdateRunTimes,
            new { Id = scheduleId, LastRunAt = lastRunAt, NextRunAt = nextRunAt },
            cancellationToken: cancellationToken));
    }
}
