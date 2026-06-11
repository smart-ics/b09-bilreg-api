using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public interface ITindakanIgdDal :
    IInsert<TindakanIgdDto>,
    IUpdate<TindakanIgdDto>,
    IDelete<ITindakanIgdKey>,
    IGetData<TindakanIgdDto, ITindakanIgdKey>,
    IListData<TindakanIgdDto, IIgdVisitKey>
{
    int CountForVisit(IIgdVisitKey visit);
}

public class TindakanIgdDal : ITindakanIgdDal
{
    private readonly DatabaseOptions _opt;

    public TindakanIgdDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TindakanIgdDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TindakanIgd (
                TindakanIgdId, IgdVisitId, RegId, ReffId, Descriptions,
                Qty, Aktifitas, PpaId, PpaName, 
                CrtUser, CrtDate)
            VALUES (
                @TindakanIgdId, @IgdVisitId, @RegId, @ReffId, @Descriptions,
                @Qty, @Aktifitas, @PpaId, @PpaName, 
                @CrtUser, @CrtDate)
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TindakanIgdDto dto)
    {
        const string sql = """
            UPDATE BILRG_TindakanIgd
            SET IgdVisitId = @IgdVisitId,
                RegId = @RegId,
                ReffId = @ReffId,
                Descriptions = @Descriptions,
                Qty = @Qty,
                Aktifitas = @Aktifitas,
                PpaId = @PpaId,
                PpaName = @PpaName,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate
            WHERE TindakanIgdId = @TindakanIgdId
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITindakanIgdKey key)
    {
        const string sql = "DELETE BILRG_TindakanIgd WHERE TindakanIgdId = @TindakanIgdId";
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", key.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TindakanIgdDto GetData(ITindakanIgdKey key)
    {
        const string sql = """
            SELECT TindakanIgdId, IgdVisitId, RegId, ReffId, Descriptions,
                Qty, Aktifitas, PpaId, PpaName,
                CrtUser, CrtDate
            FROM BILRG_TindakanIgd
            WHERE TindakanIgdId = @TindakanIgdId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", key.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanIgdDto>(sql, dp);
    }

    public IEnumerable<TindakanIgdDto> ListData(IIgdVisitKey filter)
    {
        const string sql = """
            SELECT TindakanIgdId, IgdVisitId, RegId, ReffId, Descriptions,
                Qty, Aktifitas, PpaId, PpaName, 
                CrtUser, CrtDate
            FROM BILRG_TindakanIgd
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY CrtDate
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", filter.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanIgdDto>(sql, dp);
    }

    public int CountForVisit(IIgdVisitKey visit)
    {
        const string sql = "SELECT COUNT(1) FROM BILRG_TindakanIgd WHERE IgdVisitId = @IgdVisitId";
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", visit.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp);
    }

    private static DynamicParameters BuildParams(TindakanIgdDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", dto.TindakanIgdId, SqlDbType.VarChar);
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@Descriptions", dto.Descriptions, SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty, SqlDbType.Int);
        dp.AddParam("@Aktifitas", dto.Aktifitas, SqlDbType.Int);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        return dp;
    }
}
