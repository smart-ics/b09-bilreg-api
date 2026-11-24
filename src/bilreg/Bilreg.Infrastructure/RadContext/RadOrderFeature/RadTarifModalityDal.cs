using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.RadContext.RadOrderFeature;

public class RadTarifModalityDal : IRadTarifModalityDal
{
    private const string SelectColumns = """
        TarifId, TarifName, Modality, IsActive,
        CrtUser, CrtDate, UpdUser, UpdDate
        """;

    private readonly DatabaseOptions _opt;

    public RadTarifModalityDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RadTarifModalityDto? GetData(string tarifId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadTarifModality
            WHERE TarifId = @TarifId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", tarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<RadTarifModalityDto>(sql, dp);
    }

    public IEnumerable<RadTarifModalityDto> ListByModality(string modality)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadTarifModality
            WHERE Modality = @Modality AND IsActive = 1
            ORDER BY TarifName
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Modality", modality, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RadTarifModalityDto>(sql, dp) ?? Enumerable.Empty<RadTarifModalityDto>();
    }

    public IEnumerable<RadTarifModalityDto> ListActive()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RadTarifModality
            WHERE IsActive = 1
            ORDER BY Modality, TarifName
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RadTarifModalityDto>(sql) ?? Enumerable.Empty<RadTarifModalityDto>();
    }

    public void Insert(RadTarifModalityDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RadTarifModality (
                TarifId, TarifName, Modality, IsActive,
                CrtUser, CrtDate, UpdUser, UpdDate
            ) VALUES (
                @TarifId, @TarifName, @Modality, @IsActive,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(RadTarifModalityDto dto)
    {
        const string sql = """
            UPDATE BILRG_RadTarifModality
            SET TarifName = @TarifName,
                Modality = @Modality,
                IsActive = @IsActive,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE TarifId = @TarifId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    private static DynamicParameters BuildParams(RadTarifModalityDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@Modality", dto.Modality, SqlDbType.VarChar);
        dp.AddParam("@IsActive", dto.IsActive, SqlDbType.Bit);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
