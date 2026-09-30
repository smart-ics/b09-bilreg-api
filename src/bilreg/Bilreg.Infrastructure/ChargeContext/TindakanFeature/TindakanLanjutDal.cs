using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanLanjutDal :
    IInsert<TindakanLanjutDto>,
    IUpdate<TindakanLanjutDto>,
    IGetData<TindakanLanjutDto, ITindakanLanjutKey>,
    IListData<TindakanLanjutDto, IRegKey>
{
    int UpdateState(TindakanLanjutDto dto, int expectedRowVersion);
    IEnumerable<TindakanLanjutDto> ListOutstanding(IRegKey regKey);
    IEnumerable<TindakanLanjutDto> ListOutstandingAll();
}

public interface ITindakanLanjutItemDal :
    IInsertBulk<TindakanLanjutItemDto>,
    IDelete<ITindakanLanjutKey>,
    IListData<TindakanLanjutItemDto, ITindakanLanjutKey>
{
}

public class TindakanLanjutDal : ITindakanLanjutDal
{
    private readonly DatabaseOptions _opt;

    public TindakanLanjutDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TindakanLanjutDto dto)
    {
        const string sql = """
            INSERT INTO trs_tindakan_lanjut(
                TindakanLanjutId, TindakanLanjutDate,
                RegId, PasienId, PasienName,
                OrderType, LayananId, LayananName,
                OrderState, SentDate, ReceivedDate, ReceivedBy, CancelReason, RowVersion,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @TindakanLanjutId, @TindakanLanjutDate,
                @RegId, @PasienId, @PasienName,
                @OrderType, @LayananId, @LayananName,
                @OrderState, @SentDate, @ReceivedDate, @ReceivedBy, @CancelReason, @RowVersion,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParam(dto));
    }

    public void Update(TindakanLanjutDto dto)
    {
        const string sql = """
            UPDATE trs_tindakan_lanjut
            SET
                TindakanLanjutDate = @TindakanLanjutDate,
                RegId = @RegId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                OrderType = @OrderType,
                LayananId = @LayananId,
                LayananName = @LayananName,
                OrderState = @OrderState,
                SentDate = @SentDate,
                ReceivedDate = @ReceivedDate,
                ReceivedBy = @ReceivedBy,
                CancelReason = @CancelReason,
                RowVersion = @RowVersion,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                TindakanLanjutId = @TindakanLanjutId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParam(dto));
    }

    //  Optimistic state write: validates current state before writing
    //  (late cancel cannot overwrite a confirmed reception) and bumps
    //  the RowVersion token in the same statement. Returns affected rows.
    public int UpdateState(TindakanLanjutDto dto, int expectedRowVersion)
    {
        const string sql = """
            UPDATE trs_tindakan_lanjut
            SET
                OrderState = @OrderState,
                SentDate = @SentDate,
                ReceivedDate = @ReceivedDate,
                ReceivedBy = @ReceivedBy,
                CancelReason = @CancelReason,
                RowVersion = @RowVersion,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                TindakanLanjutId = @TindakanLanjutId
                AND RowVersion = @ExpectedRowVersion
            """;

        var dp = BuildParam(dto);
        dp.AddParam("@ExpectedRowVersion", expectedRowVersion, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public TindakanLanjutDto GetData(ITindakanLanjutKey key)
    {
        const string sql = """
            SELECT
                TindakanLanjutId, TindakanLanjutDate,
                RegId, PasienId, PasienName,
                OrderType, LayananId, LayananName,
                OrderState, SentDate, ReceivedDate, ReceivedBy, CancelReason, RowVersion,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM
                trs_tindakan_lanjut
            WHERE
                TindakanLanjutId = @TindakanLanjutId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanLanjutId", key.TindakanLanjutId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanLanjutDto>(sql, dp);
    }

    public IEnumerable<TindakanLanjutDto> ListData(IRegKey filter)
    {
        const string sql = """
            SELECT
                TindakanLanjutId, TindakanLanjutDate,
                RegId, PasienId, PasienName,
                OrderType, LayananId, LayananName,
                OrderState, SentDate, ReceivedDate, ReceivedBy, CancelReason, RowVersion,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM
                trs_tindakan_lanjut
            WHERE
                RegId = @RegId
            ORDER BY
                TindakanLanjutDate, TindakanLanjutId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanLanjutDto>(sql, dp);
    }

    public IEnumerable<TindakanLanjutDto> ListOutstanding(IRegKey regKey)
    {
        const string sql = """
            SELECT
                TindakanLanjutId, TindakanLanjutDate,
                RegId, PasienId, PasienName,
                OrderType, LayananId, LayananName,
                OrderState, SentDate, ReceivedDate, ReceivedBy, CancelReason, RowVersion,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM
                trs_tindakan_lanjut
            WHERE
                RegId = @RegId
                AND OrderState IN (0, 1)
            ORDER BY
                TindakanLanjutDate, TindakanLanjutId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanLanjutDto>(sql, dp);
    }

    public IEnumerable<TindakanLanjutDto> ListOutstandingAll()
    {
        const string sql = """
            SELECT
                TindakanLanjutId, TindakanLanjutDate,
                RegId, PasienId, PasienName,
                OrderType, LayananId, LayananName,
                OrderState, SentDate, ReceivedDate, ReceivedBy, CancelReason, RowVersion,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM
                trs_tindakan_lanjut
            WHERE
                OrderState IN (0, 1)
            ORDER BY
                TindakanLanjutDate, TindakanLanjutId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanLanjutDto>(sql, new DynamicParameters());
    }

    private static DynamicParameters BuildParam(TindakanLanjutDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanLanjutId", dto.TindakanLanjutId, SqlDbType.VarChar);
        dp.AddParam("@TindakanLanjutDate", dto.TindakanLanjutDate, SqlDbType.DateTime);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@OrderType", dto.OrderType, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@OrderState", dto.OrderState, SqlDbType.Int);
        dp.AddParam("@SentDate", dto.SentDate, SqlDbType.DateTime);
        dp.AddParam("@ReceivedDate", dto.ReceivedDate, SqlDbType.DateTime);
        dp.AddParam("@ReceivedBy", dto.ReceivedBy, SqlDbType.VarChar);
        dp.AddParam("@CancelReason", dto.CancelReason, SqlDbType.VarChar);
        dp.AddParam("@RowVersion", dto.RowVersion, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}

public class TindakanLanjutItemDal : ITindakanLanjutItemDal
{
    private readonly DatabaseOptions _opt;

    public TindakanLanjutItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TindakanLanjutItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("TindakanLanjutId", "TindakanLanjutId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("ItemCode", "ItemCode");
        bcp.AddMap("ItemName", "ItemName");
        bcp.AddMap("Qty", "Qty");
        bcp.AddMap("Note", "Note");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "trs_tindakan_lanjut_item";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ITindakanLanjutKey key)
    {
        const string sql = """
            DELETE FROM
                trs_tindakan_lanjut_item
            WHERE
                TindakanLanjutId = @TindakanLanjutId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanLanjutId", key.TindakanLanjutId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TindakanLanjutItemDto> ListData(ITindakanLanjutKey filter)
    {
        const string sql = """
            SELECT
                TindakanLanjutId, ItemNo, ItemCode, ItemName, Qty, Note
            FROM
                trs_tindakan_lanjut_item
            WHERE
                TindakanLanjutId = @TindakanLanjutId
            ORDER BY
                ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanLanjutId", filter.TindakanLanjutId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanLanjutItemDto>(sql, dp);
    }
}
