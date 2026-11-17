using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public interface IMutasiBrgDal :
    IInsert<MutasiBrgDto>,
    IUpdate<MutasiBrgDto>,
    IDelete<IMutasiBrgKey>,
    IGetData<MutasiBrgDto, IMutasiBrgKey>,
    IListData<MutasiBrgDto, Periode>
{
    IEnumerable<MutasiBrgDto> ListInTransit(string layananTujuanId);
    void Insert(MutasiBrgDto dto, IDbConnection conn, IDbTransaction? trans = null);
    void Update(MutasiBrgDto dto, IDbConnection conn, IDbTransaction? trans = null);
    void Delete(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null);
    MutasiBrgDto GetData(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null);
}

public class MutasiBrgDal : IMutasiBrgDal
{
    private readonly DatabaseOptions _opt;

    public MutasiBrgDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(MutasiBrgDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Insert(dto, conn, null);
    }

    public void Insert(MutasiBrgDto dto, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            INSERT INTO BILRG_MutasiBrg (
                MutasiBrgId, MutasiBrgDate, State,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                OrderMutasiId, DispatchNote, IssuerUserId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @MutasiBrgId, @MutasiBrgDate, @State,
                @LayananAsalId, @LayananAsalName,
                @LayananTujuanId, @LayananTujuanName,
                @OrderMutasiId, @DispatchNote, @IssuerUserId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", dto.MutasiBrgId, SqlDbType.VarChar);
        dp.AddParam("@MutasiBrgDate", dto.MutasiBrgDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@LayananAsalId", dto.LayananAsalId, SqlDbType.VarChar);
        dp.AddParam("@LayananAsalName", dto.LayananAsalName, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanId", dto.LayananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanName", dto.LayananTujuanName, SqlDbType.VarChar);
        dp.AddParam("@OrderMutasiId", dto.OrderMutasiId, SqlDbType.VarChar);
        dp.AddParam("@DispatchNote", dto.DispatchNote, SqlDbType.VarChar);
        dp.AddParam("@IssuerUserId", dto.IssuerUserId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        conn.Execute(sql, dp, trans);
    }

    public void Update(MutasiBrgDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Update(dto, conn, null);
    }

    public void Update(MutasiBrgDto dto, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            UPDATE BILRG_MutasiBrg SET
                MutasiBrgDate = @MutasiBrgDate,
                State = @State,
                LayananAsalId = @LayananAsalId,
                LayananAsalName = @LayananAsalName,
                LayananTujuanId = @LayananTujuanId,
                LayananTujuanName = @LayananTujuanName,
                OrderMutasiId = @OrderMutasiId,
                DispatchNote = @DispatchNote,
                IssuerUserId = @IssuerUserId,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE 
                MutasiBrgId = @MutasiBrgId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", dto.MutasiBrgId, SqlDbType.VarChar);
        dp.AddParam("@MutasiBrgDate", dto.MutasiBrgDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@LayananAsalId", dto.LayananAsalId, SqlDbType.VarChar);
        dp.AddParam("@LayananAsalName", dto.LayananAsalName, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanId", dto.LayananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanName", dto.LayananTujuanName, SqlDbType.VarChar);
        dp.AddParam("@OrderMutasiId", dto.OrderMutasiId, SqlDbType.VarChar);
        dp.AddParam("@DispatchNote", dto.DispatchNote, SqlDbType.VarChar);
        dp.AddParam("@IssuerUserId", dto.IssuerUserId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        conn.Execute(sql, dp, trans);
    }

    public void Delete(IMutasiBrgKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        Delete(key, conn, null);
    }

    public void Delete(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            DELETE FROM BILRG_MutasiBrg WHERE MutasiBrgId = @MutasiBrgId;
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", key.MutasiBrgId, SqlDbType.VarChar);

        conn.Execute(sql, dp, trans);
    }

    public MutasiBrgDto GetData(IMutasiBrgKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return GetData(key, conn, null);
    }

    public MutasiBrgDto GetData(IMutasiBrgKey key, IDbConnection conn, IDbTransaction? trans = null)
    {
        const string sql = """
            SELECT 
                MutasiBrgId, MutasiBrgDate, State,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                OrderMutasiId, DispatchNote, IssuerUserId,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM 
                BILRG_MutasiBrg
            WHERE 
                MutasiBrgId = @MutasiBrgId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@MutasiBrgId", key.MutasiBrgId, SqlDbType.VarChar);

        return conn.ReadSingle<MutasiBrgDto>(sql, dp);
    }

    public IEnumerable<MutasiBrgDto> ListData(Periode filter)
    {
        const string sql = """
            SELECT 
                MutasiBrgId, MutasiBrgDate, State,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                OrderMutasiId, DispatchNote, IssuerUserId,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM 
                BILRG_MutasiBrg
            WHERE 
                MutasiBrgDate BETWEEN @Tgl1 AND @Tgl2
            ORDER BY
                MutasiBrgDate
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBrgDto>(sql, dp) ?? [];
    }

    public IEnumerable<MutasiBrgDto> ListInTransit(string layananTujuanId)
    {
        var sql = """
            SELECT 
                MutasiBrgId, MutasiBrgDate, State,
                LayananAsalId, LayananAsalName,
                LayananTujuanId, LayananTujuanName,
                OrderMutasiId, DispatchNote, IssuerUserId,
                CrtUser, CrtDate, UpdUser, UpdDate,
                ISNULL(VodUser, '') AS VodUser,
                ISNULL(VodDate, '3000-01-01') AS VodDate
            FROM 
                BILRG_MutasiBrg
            WHERE 
                State = 1
            """;
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(layananTujuanId))
        {
            sql += " AND LayananTujuanId = @LayananTujuanId";
            dp.AddParam("@LayananTujuanId", layananTujuanId, SqlDbType.VarChar);
        }
        sql += " ORDER BY MutasiBrgDate";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBrgDto>(sql, dp) ?? [];
    }
}
