using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public interface IDeliveryInspectionDal :
    IInsert<DeliveryInspectionDto>,
    IInsertBulk<DeliveryInspectionDto>,
    IUpdate<DeliveryInspectionDto>,
    IDelete<IDeliveryOrderKey>,
    IListData<DeliveryInspectionDto, IDeliveryOrderKey>
{
    DeliveryInspectionDto? GetData(IDeliveryOrderKey key, int noUrut);
}

public class DeliveryInspectionDal : IDeliveryInspectionDal
{
    private readonly DatabaseOptions _opt;

    public DeliveryInspectionDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DeliveryInspectionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_DoInspeksi (
                DoId, NoUrut, BrgId, PoId, PoNoUrut,
                QtyKirim, QtyDiterima, QtyDitolak, AlasanTolak,
                IsBonus, HasJaminanRetur, NoSuratJaminan, Status,
                ConfirmedAt, ConfirmedBy, VoidedAt, VoidedBy,
                CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            ) VALUES (
                @DoId, @NoUrut, @BrgId, @PoId, @PoNoUrut,
                @QtyKirim, @QtyDiterima, @QtyDitolak, @AlasanTolak,
                @IsBonus, @HasJaminanRetur, @NoSuratJaminan, @Status,
                @ConfirmedAt, @ConfirmedBy, @VoidedAt, @VoidedBy,
                @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
            )
            """;

        var dp = BuildWriteParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<DeliveryInspectionDto> listModel)
    {
        var items = listModel?.ToList() ?? new List<DeliveryInspectionDto>();
        if (items.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_DoInspeksi (
                DoId, NoUrut, BrgId, PoId, PoNoUrut,
                QtyKirim, QtyDiterima, QtyDitolak, AlasanTolak,
                IsBonus, HasJaminanRetur, NoSuratJaminan, Status,
                ConfirmedAt, ConfirmedBy, VoidedAt, VoidedBy,
                CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            ) VALUES (
                @DoId, @NoUrut, @BrgId, @PoId, @PoNoUrut,
                @QtyKirim, @QtyDiterima, @QtyDitolak, @AlasanTolak,
                @IsBonus, @HasJaminanRetur, @NoSuratJaminan, @Status,
                @ConfirmedAt, @ConfirmedBy, @VoidedAt, @VoidedBy,
                @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var item in items)
        {
            var dp = BuildWriteParams(item);
            conn.Execute(sql, dp);
        }
    }

    public void Update(DeliveryInspectionDto dto)
    {
        const string sql = """
            UPDATE BILRG_DoInspeksi SET
                BrgId = @BrgId,
                PoId = @PoId,
                PoNoUrut = @PoNoUrut,
                QtyKirim = @QtyKirim,
                QtyDiterima = @QtyDiterima,
                QtyDitolak = @QtyDitolak,
                AlasanTolak = @AlasanTolak,
                IsBonus = @IsBonus,
                HasJaminanRetur = @HasJaminanRetur,
                NoSuratJaminan = @NoSuratJaminan,
                Status = @Status,
                ConfirmedAt = @ConfirmedAt,
                ConfirmedBy = @ConfirmedBy,
                VoidedAt = @VoidedAt,
                VoidedBy = @VoidedBy,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE
                DoId = @DoId AND NoUrut = @NoUrut
            """;

        var dp = BuildWriteParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IDeliveryOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_DoInspeksi
            WHERE DoId = @DoId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DoId", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<DeliveryInspectionDto> ListData(IDeliveryOrderKey filter)
    {
        var sql = $"""
            {SelectClause()}
            WHERE aa.DoId = @DoId
            ORDER BY aa.NoUrut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DoId", filter.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DeliveryInspectionDto>(sql, dp) ?? Enumerable.Empty<DeliveryInspectionDto>();
    }

    public DeliveryInspectionDto? GetData(IDeliveryOrderKey key, int noUrut)
    {
        var sql = $"""
            {SelectClause()}
            WHERE aa.DoId = @DoId AND aa.NoUrut = @NoUrut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DoId", key.DeliveryOrderId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<DeliveryInspectionDto>(sql, dp);
    }

    private static string SelectClause() => """
        SELECT
            aa.DoId,
            aa.NoUrut,
            aa.BrgId,
            ISNULL(bb.fs_nm_barang, '') AS BrgName,
            aa.PoId,
            aa.PoNoUrut,
            aa.QtyKirim,
            aa.QtyDiterima,
            aa.QtyDitolak,
            aa.AlasanTolak,
            aa.IsBonus,
            aa.HasJaminanRetur,
            aa.NoSuratJaminan,
            aa.Status,
            aa.ConfirmedAt,
            aa.ConfirmedBy,
            aa.VoidedAt,
            aa.VoidedBy,
            aa.CreatedAt,
            aa.CreatedBy,
            aa.UpdatedAt,
            aa.UpdatedBy
        FROM BILRG_DoInspeksi aa
        LEFT JOIN tb_barang bb ON aa.BrgId = bb.fs_kd_barang
        """;

    private static DynamicParameters BuildWriteParams(DeliveryInspectionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@DoId", dto.DoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@BrgId", dto.BrgId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PoId", dto.PoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PoNoUrut", dto.PoNoUrut, SqlDbType.Int);
        dp.AddParam("@QtyKirim", dto.QtyKirim, SqlDbType.Decimal);
        dp.AddParam("@QtyDiterima", dto.QtyDiterima, SqlDbType.Decimal);
        dp.AddParam("@QtyDitolak", dto.QtyDitolak, SqlDbType.Decimal);
        dp.AddParam("@AlasanTolak", dto.AlasanTolak ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@IsBonus", dto.IsBonus, SqlDbType.Bit);
        dp.AddParam("@HasJaminanRetur", dto.HasJaminanRetur, SqlDbType.Bit);
        dp.AddParam("@NoSuratJaminan", dto.NoSuratJaminan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Status", string.IsNullOrWhiteSpace(dto.Status) ? "Draft" : dto.Status, SqlDbType.VarChar);
        dp.AddParam("@ConfirmedAt", dto.ConfirmedAt, SqlDbType.DateTime);
        dp.AddParam("@ConfirmedBy", dto.ConfirmedBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@VoidedAt", dto.VoidedAt, SqlDbType.DateTime);
        dp.AddParam("@VoidedBy", dto.VoidedBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CreatedAt", dto.CreatedAt != default ? dto.CreatedAt : DateTime.Now, SqlDbType.DateTime);
        dp.AddParam("@CreatedBy", dto.CreatedBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UpdatedAt", dto.UpdatedAt != default ? dto.UpdatedAt : DateTime.Now, SqlDbType.DateTime);
        dp.AddParam("@UpdatedBy", dto.UpdatedBy ?? string.Empty, SqlDbType.VarChar);
        return dp;
    }
}
