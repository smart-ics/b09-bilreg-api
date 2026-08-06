using Dapper;
using Bilreg.Domain.BrgContext.StandardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BrgContext.StandardFeature;

public interface IKfaDosageUsageDal :
    IListData<KfaDosageUsageDto, IKfaKey>
{
}

public class KfaDosageUsageDal : IKfaDosageUsageDal
{
    private readonly DatabaseOptions _opt;

    public KfaDosageUsageDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<KfaDosageUsageDto> ListData(IKfaKey filter)
    {
        const string sql = """
            SELECT
                KfaId, DisplayName, Category, BodyWeightMax,
                BodyWeightMin, Duration, DurationMax, DurationUcum,
                Frequency, FrequencyMax, Period, PeriodUcum,
                Qty, QtyHigh, QtyUcum, QtyUom, UseUcum
            FROM 
                FARPU_KfaDosageUsage
            WHERE 
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", filter.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KfaDosageUsageDto>(sql, dp);
    }
}