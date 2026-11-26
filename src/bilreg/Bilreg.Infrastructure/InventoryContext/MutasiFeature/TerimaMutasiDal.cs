using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public interface ITerimaMutasiDal :
    IInsert<TerimaMutasiDto>,
    IUpdate<TerimaMutasiDto>,
    IDelete<ITerimaMutasiKey>,
    IGetData<TerimaMutasiDto, ITerimaMutasiKey>
{
    void Insert(TerimaMutasiDto dto, IDbConnection conn, IDbTransaction? trans = null);
    void Update(TerimaMutasiDto dto, IDbConnection conn, IDbTransaction? trans = null);
    void Delete(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null);
    TerimaMutasiDto GetData(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null);
    TerimaMutasiDto GetByMutasiBrgId(string mutasiBrgId);
    IEnumerable<TerimaMutasiDto> ListData(Periode filter, string layananTujuanId = "");
}

public class TerimaMutasiDal : ITerimaMutasiDal
{
    private readonly DatabaseOptions _opt;

    public TerimaMutasiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TerimaMutasiDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Insert(dto, conn, null);
    }

    public void Insert(TerimaMutasiDto dto, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            INSERT INTO BILRG_TerimaMutasi (
                TerimaMutasiId, TerimaMutasiDate, State,
                MutasiBrgId, OrderMutasiId,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                ReceiverUserId, Note, RejectionReason,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @TerimaMutasiId, @TerimaMutasiDate, @State,
                @MutasiBrgId, @OrderMutasiId,
                @LayananAsalId, @LayananAsalName,
                @LayananTujuanId, @LayananTujuanName,
                @ReceiverUserId, @Note, @RejectionReason,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var dp = CreateParameters(dto);
        conn.Execute(sql, dp, trans);
    }

    public void Update(TerimaMutasiDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Update(dto, conn, null);
    }

    public void Update(TerimaMutasiDto dto, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            UPDATE BILRG_TerimaMutasi SET
                TerimaMutasiDate = @TerimaMutasiDate,
                State = @State,
                MutasiBrgId = @MutasiBrgId,
                OrderMutasiId = @OrderMutasiId,
                LayananAsalId = @LayananAsalId,
                LayananAsalName = @LayananAsalName,
                LayananTujuanId = @LayananTujuanId,
                LayananTujuanName = @LayananTujuanName,
                ReceiverUserId = @ReceiverUserId,
                Note = @Note,
                RejectionReason = @RejectionReason,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                TerimaMutasiId = @TerimaMutasiId
            """;
        var dp = CreateParameters(dto);
        conn.Execute(sql, dp, trans);
    }

    public void Delete(ITerimaMutasiKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Delete(key, conn, null);
    }

    public void Delete(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            DELETE FROM BILRG_TerimaMutasi WHERE TerimaMutasiId = @TerimaMutasiId;
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TerimaMutasiId", key.TerimaMutasiId, SqlDbType.VarChar);

        conn.Execute(sql, dp, trans);
    }

    public TerimaMutasiDto GetData(ITerimaMutasiKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return GetData(key, conn, null);
    }

    public TerimaMutasiDto GetData(ITerimaMutasiKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            SELECT
                TerimaMutasiId, TerimaMutasiDate, State,
                MutasiBrgId, OrderMutasiId,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                ReceiverUserId, Note, RejectionReason,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM
                BILRG_TerimaMutasi
            WHERE
                TerimaMutasiId = @TerimaMutasiId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TerimaMutasiId", key.TerimaMutasiId, SqlDbType.VarChar);

        return conn.QueryFirstOrDefault<TerimaMutasiDto>(sql, dp, trans)!;
    }

    public TerimaMutasiDto GetByMutasiBrgId(string mutasiBrgId)
    {
        const string sql = """
            SELECT
                TerimaMutasiId, TerimaMutasiDate, State,
                MutasiBrgId, OrderMutasiId,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                ReceiverUserId, Note, RejectionReason,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM
                BILRG_TerimaMutasi
            WHERE
                MutasiBrgId = @MutasiBrgId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", mutasiBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<TerimaMutasiDto>(sql, dp)!;
    }

    public IEnumerable<TerimaMutasiDto> ListData(Periode filter, string layananTujuanId = "")
    {
        var sql = """
            SELECT
                TerimaMutasiId, TerimaMutasiDate, State,
                MutasiBrgId, OrderMutasiId,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                ReceiverUserId, Note, RejectionReason,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM
                BILRG_TerimaMutasi
            WHERE
                TerimaMutasiDate BETWEEN @Tgl1 AND @Tgl2
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);

        if (!string.IsNullOrWhiteSpace(layananTujuanId))
        {
            sql += " AND LayananTujuanId = @LayananTujuanId";
            dp.AddParam("@LayananTujuanId", layananTujuanId.Trim(), SqlDbType.VarChar);
        }

        sql += " ORDER BY TerimaMutasiDate";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TerimaMutasiDto>(sql, dp) ?? [];
    }

    private static DynamicParameters CreateParameters(TerimaMutasiDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TerimaMutasiId", dto.TerimaMutasiId, SqlDbType.VarChar);
        dp.AddParam("@TerimaMutasiDate", dto.TerimaMutasiDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@MutasiBrgId", dto.MutasiBrgId, SqlDbType.VarChar);
        dp.AddParam("@OrderMutasiId", dto.OrderMutasiId, SqlDbType.VarChar);
        dp.AddParam("@LayananAsalId", dto.LayananAsalId, SqlDbType.VarChar);
        dp.AddParam("@LayananAsalName", dto.LayananAsalName, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanId", dto.LayananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanName", dto.LayananTujuanName, SqlDbType.VarChar);
        dp.AddParam("@ReceiverUserId", dto.ReceiverUserId, SqlDbType.VarChar);
        dp.AddParam("@Note", dto.Note, SqlDbType.VarChar);
        dp.AddParam("@RejectionReason", dto.RejectionReason, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
