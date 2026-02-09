using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpDal :
    IInsert<OrderOpDto>,
    IUpdate<OrderOpDto>,
    IDelete<IOrderOpKey>,
    IGetData<OrderOpDto, IOrderOpKey>,
    IListData<OrderOpDto, Periode>
{
}

public class OrderOpDal : IOrderOpDal
{
    public readonly DatabaseOptions _opt;

    public OrderOpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderOpDto model)
    {
        const string sql = """
           INSERT INTO BILRG_OrderOp(
               OrderOpId, OrderDate, RegId, PasienId,
               Icd10Id, JenisOperasiId, NamaOperasi,
               DokterId, UrgencyLevel, TarifId, TarifName,
               EstimasiDurasi, PreferedDate, SpecialEquipment,
               CrtUser, CrtDate,
               UpdUser, UpdDate,
               VodUser, VodDate)
           VALUES(
               @OrderOpId, @OrderDate, @RegId, @PasienId,
               @Icd10Id, @JenisOperasiId, @NamaOperasi,
               @DokterId, @UrgencyLevel, @TarifId, @TarifName,
               @EstimasiDurasi, @PreferedDate, @SpecialEquipment,
               @CrtUser, @CrtDate,
               @UpdUser, @UpdDate,
               @VodUser, @VodDate)
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", model.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", model.OrderDate, SqlDbType.DateTime);
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@Icd10Id", model.Icd10Id, SqlDbType.VarChar);
        dp.AddParam("@JenisOperasiId", model.JenisOperasiId, SqlDbType.VarChar);
        dp.AddParam("@NamaOperasi", model.NamaOperasi, SqlDbType.VarChar);
        
        dp.AddParam("@DokterId", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@UrgencyLevel", model.UrgencyLevel, SqlDbType.Int);
        dp.AddParam("@TarifId", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", model.TarifName, SqlDbType.VarChar);
        dp.AddParam("@EstimasiDurasi", model.EstimasiDurasi, SqlDbType.Int);
        dp.AddParam("@PreferedDate", model.PreferedDate, SqlDbType.DateTime);
        dp.AddParam("@SpecialEquipment", model.SpecialEquipment, SqlDbType.VarChar);
        
        dp.AddParam("@CrtUser", model.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", model.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", model.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", model.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", model.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", model.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OrderOpDto model)
    {
        const string sql = """
           UPDATE 
                BILRG_OrderOp
           SET
               OrderDate = @OrderDate,
               RegId = @RegId,
               PasienId = @PasienId,
               Icd10Id = @Icd10Id,
               JenisOperasiId = @JenisOperasiId,
               NamaOperasi = @NamaOperasi,
        
               DokterId = @DokterId,
               UrgencyLevel = @UrgencyLevel,
               TarifId = @TarifId,
               TarifName = @TarifName,
               EstimasiDurasi = @EstimasiDurasi,
               PreferedDate = @PreferedDate,
               SpecialEquipment = @SpecialEquipment,
        
               CrtUser = @CrtUser,
               CrtDate = @CrtDate,
               UpdUser = @UpdUser,
               UpdDate = @UpdDate,
               VodUser = @VodUser,
               VodDate = @VodDate
           WHERE
               OrderOpId = @OrderOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", model.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", model.OrderDate, SqlDbType.DateTime);
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@Icd10Id", model.Icd10Id, SqlDbType.VarChar);
        dp.AddParam("@JenisOperasiId", model.JenisOperasiId, SqlDbType.VarChar);
        dp.AddParam("@NamaOperasi", model.NamaOperasi, SqlDbType.VarChar);
        
        dp.AddParam("@DokterId", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@UrgencyLevel", model.UrgencyLevel, SqlDbType.Int);
        dp.AddParam("@TarifId", model.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", model.TarifName, SqlDbType.VarChar);
        dp.AddParam("@EstimasiDurasi", model.EstimasiDurasi, SqlDbType.Int);
        dp.AddParam("@PreferedDate", model.PreferedDate, SqlDbType.DateTime);
        dp.AddParam("@SpecialEquipment", model.SpecialEquipment, SqlDbType.VarChar);
        
        dp.AddParam("@CrtUser", model.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", model.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", model.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", model.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", model.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", model.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = """
           DELETE FROM 
                BILRG_OrderOp
           WHERE
               OrderOpId = @OrderOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OrderOpDto GetData(IOrderOpKey key)
    {
        const string sql = """
           SELECT
               aa.OrderOpId, aa.OrderDate, aa.RegId, PasienId,
               aa.Icd10Id, aa.JenisOperasiId, aa.NamaOperasi,
               aa.DokterId, aa.UrgencyLevel, aa.TarifId, aa.TarifName,
               aa.EstimasiDurasi, aa.PreferedDate, aa.SpecialEquipment,
               aa.CrtUser, aa.CrtDate,
               aa.UpdUser, aa.UpdDate,
               aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender,
               ISNULL(cc.fs_ket_icd, '') AS fs_ket_icd,
               ISNULL(dd.fs_nm_jenis_operasi, '') AS fs_nm_jenis_operasi,
               ISNULL(ee.fs_nm_peg, '') AS fs_nm_peg
               
           FROM
               BILRG_OrderOp aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
               LEFT JOIN tc_icd cc ON aa.Icd10id = cc.fs_kd_icd
               LEFT JOIN ta_jenis_operasi dd ON aa.JenisOperasiId = dd.fs_kd_jenis_operasi
               LEFT JOIN td_peg ee ON aa.DokterId = ee.fs_kd_peg
           WHERE
               OrderOpId = @OrderOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderOpDto>(sql, dp);
    }

    public IEnumerable<OrderOpDto> ListData(Periode filter)
    {
        const string sql = """
           SELECT
               aa.OrderOpId, aa.OrderDate, aa.RegId, PasienId,
               aa.Icd10Id, aa.JenisOperasiId, aa.NamaOperasi,
               aa.DokterId, aa.UrgencyLevel, aa.TarifId, aa.TarifName,
               aa.EstimasiDurasi, aa.PreferedDate, aa.SpecialEquipment,
               aa.CrtUser, aa.CrtDate,
               aa.UpdUser, aa.UpdDate,
               aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender,
               ISNULL(cc.fs_ket_icd, '') AS fs_ket_icd,
               ISNULL(dd.fs_nm_jenis_operasi, '') AS fs_nm_jenis_operasi,
               ISNULL(ee.fs_nm_peg, '') AS fs_nm_peg
               
           FROM 
               BILRG_OrderOp aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
               LEFT JOIN tc_icd cc ON aa.Icd10id = cc.fs_kd_icd
               LEFT JOIN ta_jenis_operasi dd ON aa.JenisOperasiId = dd.fs_kd_jenis_operasi
               LEFT JOIN td_peg ee ON aa.DokterId = ee.fs_kd_peg
           WHERE
               OrderDate BETWEEN @Tgl1 AND @Tgl2
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderOpDto>(sql, dp);
    }
}