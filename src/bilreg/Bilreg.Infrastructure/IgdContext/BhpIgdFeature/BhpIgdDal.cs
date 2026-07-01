using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.BhpIgdFeature;

public interface IBhpIgdDal :
    IInsert<BhpIgdDto>,
    IUpdate<BhpIgdDto>,
    IDelete<IBhpIgdKey>,
    IGetData<BhpIgdDto, IBhpIgdKey>,
    IListData<BhpIgdDto, IIgdVisitKey>
{
    int CountForVisit(IIgdVisitKey visit);
}

public class BhpIgdDal : IBhpIgdDal
{
    private readonly DatabaseOptions _opt;

    public BhpIgdDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BhpIgdDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BhpIgd (
                BhpIgdId, IgdVisitId, RegId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate)
            VALUES (
                @BhpIgdId, @IgdVisitId, @RegId, @BhpItemId, @BhpItemName,
                @Qty, @Price, @CrtUser, @CrtDate)
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BhpIgdDto dto)
    {
        const string sql = """
            UPDATE BILRG_BhpIgd
            SET IgdVisitId = @IgdVisitId,
                RegId = @RegId,
                BhpItemId = @BhpItemId,
                BhpItemName = @BhpItemName,
                Qty = @Qty,
                Price = @Price,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate
            WHERE BhpIgdId = @BhpIgdId
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBhpIgdKey key)
    {
        const string sql = "DELETE BILRG_BhpIgd WHERE BhpIgdId = @BhpIgdId";
        var dp = new DynamicParameters();
        dp.AddParam("@BhpIgdId", key.BhpIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BhpIgdDto GetData(IBhpIgdKey key)
    {
        const string sql = """
            SELECT BhpIgdId, IgdVisitId, RegId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate
            FROM BILRG_BhpIgd
            WHERE BhpIgdId = @BhpIgdId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BhpIgdId", key.BhpIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BhpIgdDto>(sql, dp);
    }

    public IEnumerable<BhpIgdDto> ListData(IIgdVisitKey filter)
    {
        const string sql = """
            SELECT BhpIgdId, IgdVisitId, RegId, BhpItemId, BhpItemName,
                Qty, Price, CrtUser, CrtDate
            FROM BILRG_BhpIgd
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY CrtDate
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", filter.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BhpIgdDto>(sql, dp);
    }

    public int CountForVisit(IIgdVisitKey visit)
    {
        const string sql = "SELECT COUNT(1) FROM BILRG_BhpIgd WHERE IgdVisitId = @IgdVisitId";
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", visit.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp);
    }

    private static DynamicParameters BuildParams(BhpIgdDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BhpIgdId", dto.BhpIgdId, SqlDbType.VarChar);
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@BhpItemId", dto.BhpItemId, SqlDbType.VarChar);
        dp.AddParam("@BhpItemName", dto.BhpItemName, SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty, SqlDbType.Int);
        dp.AddParam("@Price", dto.Price, SqlDbType.Decimal);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        return dp;
    }
}
