using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record TujuanLanjutDto(
    string OrderType, string LayananId, string LayananName, bool IsActive)
{
    public TujuanLanjutType ToModel()
    {
        return new TujuanLanjutType(OrderType, new LayananReff(LayananId, LayananName), IsActive);
    }
}

//  M03-F01 P2-S04 — Dapper read of trs_tujuan_lanjut (TD-04).
//  Read-only: rows are seeded by P1-S02; ops retarget them without code change.
public interface ITujuanLanjutDal : IListData<TujuanLanjutDto>
{
}

public class TujuanLanjutDal : ITujuanLanjutDal
{
    private readonly DatabaseOptions _opt;

    public TujuanLanjutDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<TujuanLanjutDto> ListData()
    {
        const string sql = """
            SELECT
                OrderType, LayananId, LayananName, IsActive
            FROM
                trs_tujuan_lanjut
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TujuanLanjutDto>(sql, new DynamicParameters());
    }
}
