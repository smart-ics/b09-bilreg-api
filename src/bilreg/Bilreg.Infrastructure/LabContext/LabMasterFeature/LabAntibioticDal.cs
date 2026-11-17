using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabAntibioticDal : ILabAntibioticDal
{
    private const string SelectColumns = """
        AntibioticId,
        AntibioticName,
        DiskConcentration,
        ResistenRange,
        IntermediateRange,
        SensitifRange,
        DisplayOrder,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabAntibioticDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabAntibioticDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabAntibiotic (
                AntibioticId, AntibioticName, DiskConcentration,
                ResistenRange, IntermediateRange, SensitifRange, DisplayOrder,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @AntibioticId, @AntibioticName, @DiskConcentration,
                @ResistenRange, @IntermediateRange, @SensitifRange, @DisplayOrder,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabAntibioticDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabAntibiotic
            SET AntibioticName = @AntibioticName,
                DiskConcentration = @DiskConcentration,
                ResistenRange = @ResistenRange,
                IntermediateRange = @IntermediateRange,
                SensitifRange = @SensitifRange,
                DisplayOrder = @DisplayOrder,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE AntibioticId = @AntibioticId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabAntibioticKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabAntibiotic
            WHERE AntibioticId = @AntibioticId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntibioticId", key.AntibioticId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabAntibioticDto? GetData(ILabAntibioticKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabAntibiotic
            WHERE AntibioticId = @AntibioticId
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntibioticId", key.AntibioticId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabAntibioticDto>(sql, dp);
    }

    public IEnumerable<LabAntibioticDto> ListData()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabAntibiotic
            WHERE VodUser = ''
            ORDER BY DisplayOrder, AntibioticName
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabAntibioticDto>(sql) ?? Enumerable.Empty<LabAntibioticDto>();
    }

    public IEnumerable<LabAntibioticDto> ListData(string? search)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabAntibiotic
            WHERE VodUser = ''
              AND (
                  @Search IS NULL OR @Search = ''
                  OR AntibioticId LIKE @SearchPattern
                  OR AntibioticName LIKE @SearchPattern
              )
            ORDER BY DisplayOrder, AntibioticName
            """;

        var searchPattern = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var dp = new DynamicParameters();
        dp.AddParam("@Search", search ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabAntibioticDto>(sql, dp) ?? Enumerable.Empty<LabAntibioticDto>();
    }

    private static DynamicParameters BuildParams(LabAntibioticDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@AntibioticId", dto.AntibioticId, SqlDbType.VarChar);
        dp.AddParam("@AntibioticName", dto.AntibioticName, SqlDbType.VarChar);
        dp.AddParam("@DiskConcentration", dto.DiskConcentration, SqlDbType.VarChar);
        dp.AddParam("@ResistenRange", dto.ResistenRange, SqlDbType.VarChar);
        dp.AddParam("@IntermediateRange", dto.IntermediateRange, SqlDbType.VarChar);
        dp.AddParam("@SensitifRange", dto.SensitifRange, SqlDbType.VarChar);
        dp.AddParam("@DisplayOrder", dto.DisplayOrder, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
