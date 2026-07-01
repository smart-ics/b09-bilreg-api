using Dapper;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.InventoryContext.MutasiFeature;

public interface IOrderMutasiDal :
    IInsert<OrderMutasiDto>,
    IUpdate<OrderMutasiDto>,
    IDelete<IOrderMutasiKey>,
    IGetData<OrderMutasiDto, IOrderMutasiKey>,
    IListData<OrderMutasiDto, Periode>
{
    IEnumerable<OrderMutasiDto> ListDraftState();
    IEnumerable<OrderMutasiDto> ListApprovedState(Periode filter);
}

public class OrderMutasiDal : IOrderMutasiDal
{
    private readonly DatabaseOptions _opt;

    public OrderMutasiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderMutasiDto dto)
    {
        const string sql = """
            INSERT INTO FARIN_OrderMutasi (
                OrderMutasiId, OrderMutasiDate, State,
                LayananOrderId, LayananOrderName, 
                LayananTujuanId, LayananTujuanName, 
                ApprovalUserId, ApprovalDate, 
                RejectionUserId, RejectionDate, OrderNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @OrderMutasiId, @OrderMutasiDate, @State,
                @LayananOrderId, @LayananOrderName, 
                @LayananTujuanId, @LayananTujuanName, 
                @ApprovalUserId, @ApprovalDate, 
                @RejectionUserId, @RejectionDate, @OrderNote,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            )
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderMutasiId", dto.OrderMutasiId, SqlDbType.VarChar);
        dp.AddParam("@OrderMutasiDate", dto.OrderMutasiDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@LayananOrderId", dto.LayananOrderId, SqlDbType.VarChar);
        dp.AddParam("@LayananOrderName", dto.LayananOrderName, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanId", dto.LayananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanName", dto.LayananTujuanName, SqlDbType.VarChar);
        dp.AddParam("@ApprovalUserId", dto.ApprovalUserId, SqlDbType.VarChar);
        dp.AddParam("@ApprovalDate", dto.ApprovalDate, SqlDbType.DateTime);
        dp.AddParam("@RejectionUserId", dto.RejectionUserId, SqlDbType.VarChar);
        dp.AddParam("@RejectionDate", dto.RejectionDate, SqlDbType.DateTime);
        dp.AddParam("@OrderNote", dto.OrderNote, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OrderMutasiDto dto)
    {
        const string sql = """
            UPDATE FARIN_OrderMutasi SET
                OrderMutasiDate = @OrderMutasiDate,
                State = @State,
                LayananOrderId = @LayananOrderId,
                LayananOrderName = @LayananOrderName,
                LayananTujuanId = @LayananTujuanId,
                LayananTujuanName = @LayananTujuanName,
                ApprovalUserId = @ApprovalUserId,
                ApprovalDate = @ApprovalDate,
                RejectionUserId = @RejectionUserId,
                RejectionDate = @RejectionDate,
                OrderNote = @OrderNote,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE 
                OrderMutasiId = @OrderMutasiId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderMutasiId", dto.OrderMutasiId, SqlDbType.VarChar);
        dp.AddParam("@OrderMutasiDate", dto.OrderMutasiDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@LayananOrderId", dto.LayananOrderId, SqlDbType.VarChar);
        dp.AddParam("@LayananOrderName", dto.LayananOrderName, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanId", dto.LayananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@LayananTujuanName", dto.LayananTujuanName, SqlDbType.VarChar);
        dp.AddParam("@ApprovalUserId", dto.ApprovalUserId, SqlDbType.VarChar);
        dp.AddParam("@ApprovalDate", dto.ApprovalDate, SqlDbType.DateTime);
        dp.AddParam("@RejectionUserId", dto.RejectionUserId, SqlDbType.VarChar);
        dp.AddParam("@RejectionDate", dto.RejectionDate, SqlDbType.DateTime);
        dp.AddParam("@OrderNote", dto.OrderNote, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderMutasiKey key)
    {
        const string sql = """
            DELETE FROM FARIN_OrderMutasi WHERE OrderMutasiId = @OrderMutasiId;
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderMutasiId", key.OrderMutasiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OrderMutasiDto GetData(IOrderMutasiKey key)
    {
        const string sql = """
            SELECT 
                OrderMutasiId, OrderMutasiDate, State,
                LayananOrderId, LayananOrderName, 
                LayananTujuanId, LayananTujuanName, 
                ApprovalUserId, ApprovalDate, 
                RejectionUserId, RejectionDate, OrderNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM 
                FARIN_OrderMutasi
            WHERE 
                OrderMutasiId = @OrderMutasiId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderMutasiId", key.OrderMutasiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderMutasiDto>(sql, dp);
    }

    public IEnumerable<OrderMutasiDto> ListData(Periode filter)
    {
        const string sql = """
            SELECT 
                OrderMutasiId, OrderMutasiDate, State,
                LayananOrderId, LayananOrderName, 
                LayananTujuanId, LayananTujuanName, 
                ApprovalUserId, ApprovalDate, 
                RejectionUserId, RejectionDate, OrderNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM 
                FARIN_OrderMutasi
            WHERE 
                OrderMutasiDate BETWEEN @Tgl1 AND @Tgl2
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderMutasiDto>(sql, dp);
    }

    public IEnumerable<OrderMutasiDto> ListDraftState()
    {
        const string sql = """
            SELECT 
                OrderMutasiId, OrderMutasiDate, State,
                LayananOrderId, LayananOrderName, 
                LayananTujuanId, LayananTujuanName, 
                ApprovalUserId, ApprovalDate, 
                RejectionUserId, RejectionDate, OrderNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM 
                FARIN_OrderMutasi
            WHERE 
                State = 0
            """;
        var dp = new DynamicParameters();
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderMutasiDto>(sql, dp);
    }

    public IEnumerable<OrderMutasiDto> ListApprovedState(Periode filter)
    {
        const string sql = """
            SELECT 
                OrderMutasiId, OrderMutasiDate, State,
                LayananOrderId, LayananOrderName, 
                LayananTujuanId, LayananTujuanName, 
                ApprovalUserId, ApprovalDate, 
                RejectionUserId, RejectionDate, OrderNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM 
                FARIN_OrderMutasi
            WHERE 
                ApprovalDate BETWEEN @Tgl1 AND @Tgl2
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderMutasiDto>(sql, dp);
    }
}
