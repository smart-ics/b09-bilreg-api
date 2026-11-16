using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IScheduleOpHistDal :
    IInsert<ScheduleOpHistDto>,
    IInsertBulk<ScheduleOpHistDto>,
    IDelete<IScheduleOpKey>,
    IListData<ScheduleOpHistDto, IScheduleOpKey>
{
}

public class ScheduleOpHistDal : IScheduleOpHistDal
{
    private readonly DatabaseOptions _opt;

    public ScheduleOpHistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ScheduleOpHistDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_ScheduleOpHist(
                ScheduleOpHistId, ScheduleOpId, NoUrut,
                OldTglOp, NewTglOp, OldDurasi, NewDurasi,
                OldKamarId, NewKamarId, OldPpaId, NewPpaId,
                Alasan, IsOverride, OverrideReason, UserId, CrtDate, ChangeType)
            VALUES(
                @ScheduleOpHistId, @ScheduleOpId, @NoUrut,
                @OldTglOp, @NewTglOp, @OldDurasi, @NewDurasi,
                @OldKamarId, @NewKamarId, @OldPpaId, @NewPpaId,
                @Alasan, @IsOverride, @OverrideReason, @UserId, @CrtDate, @ChangeType)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpHistId", dto.ScheduleOpHistId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@OldTglOp", dto.OldTglOp, SqlDbType.DateTime);
        dp.AddParam("@NewTglOp", dto.NewTglOp, SqlDbType.DateTime);
        dp.AddParam("@OldDurasi", dto.OldDurasi, SqlDbType.Int);
        dp.AddParam("@NewDurasi", dto.NewDurasi, SqlDbType.Int);
        dp.AddParam("@OldKamarId", dto.OldKamarId, SqlDbType.VarChar);
        dp.AddParam("@NewKamarId", dto.NewKamarId, SqlDbType.VarChar);
        dp.AddParam("@OldPpaId", dto.OldPpaId, SqlDbType.VarChar);
        dp.AddParam("@NewPpaId", dto.NewPpaId, SqlDbType.VarChar);
        dp.AddParam("@Alasan", dto.Alasan, SqlDbType.VarChar);
        dp.AddParam("@IsOverride", dto.IsOverride, SqlDbType.Bit);
        dp.AddParam("@OverrideReason", dto.OverrideReason, SqlDbType.VarChar);
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@ChangeType", dto.ChangeType, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<ScheduleOpHistDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ScheduleOpHistId", "ScheduleOpHistId");
        bcp.AddMap("ScheduleOpId", "ScheduleOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("OldTglOp", "OldTglOp");
        bcp.AddMap("NewTglOp", "NewTglOp");
        bcp.AddMap("OldDurasi", "OldDurasi");
        bcp.AddMap("NewDurasi", "NewDurasi");
        bcp.AddMap("OldKamarId", "OldKamarId");
        bcp.AddMap("NewKamarId", "NewKamarId");
        bcp.AddMap("OldPpaId", "OldPpaId");
        bcp.AddMap("NewPpaId", "NewPpaId");
        bcp.AddMap("Alasan", "Alasan");
        bcp.AddMap("IsOverride", "IsOverride");
        bcp.AddMap("OverrideReason", "OverrideReason");
        bcp.AddMap("UserId", "UserId");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("ChangeType", "ChangeType");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_ScheduleOpHist";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IScheduleOpKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_ScheduleOpHist
            WHERE
                ScheduleOpId = @ScheduleOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", key.ScheduleOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ScheduleOpHistDto> ListData(IScheduleOpKey filter)
    {
        const string sql = """
            SELECT
                ScheduleOpHistId,
                ScheduleOpId,
                NoUrut,
                OldTglOp,
                NewTglOp,
                OldDurasi,
                NewDurasi,
                OldKamarId,
                NewKamarId,
                OldPpaId,
                NewPpaId,
                Alasan,
                IsOverride,
                OverrideReason,
                UserId,
                CrtDate,
                ChangeType
            FROM
                BILRG_ScheduleOpHist
            WHERE
                ScheduleOpId = @ScheduleOpId
            ORDER BY
                NoUrut ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", filter.ScheduleOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ScheduleOpHistDto>(sql, dp) ?? [];
    }
}
