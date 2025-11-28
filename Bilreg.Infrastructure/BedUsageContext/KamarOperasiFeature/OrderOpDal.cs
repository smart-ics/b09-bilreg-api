using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

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
               DokterId, EstimasiDurasi, PreferedDate,
               SpecialEquipment, OrderOpState,
               CreateUserId, CreateTimestamp,
               UpdateUserId, UpdateTimestamp,
               VoidUserId, VoidTimestamp)
           VALUES(
               @OrderOpId, @OrderDate, @RegId, @PasienId,
               @Icd10Id, @JenisOperasiId, @NamaOperasi,
               @DokterId, @EstimasiDurasi, @PreferedDate,
               @SpecialEquipment, @OrderOpState,
               @CreateUserId, @CreateTimestamp,
               @UpdateUserId, @UpdateTimestamp,
               @VoidUserId, @VoidTimestamp)
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
        dp.AddParam("@EstimasiDurasi", model.EstimasiDurasi, SqlDbType.Int);
        dp.AddParam("@PreferedDate", model.PreferedDate, SqlDbType.DateTime);
        dp.AddParam("@SpecialEquipment", model.SpecialEquipment, SqlDbType.VarChar);
        dp.AddParam("@OrderOpState", model.OrderOpState, SqlDbType.Int);
        dp.AddParam("@CreateUserId", model.CreateUserId, SqlDbType.VarChar);
        dp.AddParam("@CreateTimestamp", model.CreateTimestamp, SqlDbType.DateTime);
        dp.AddParam("@UpdateUserId", model.UpdateUserId, SqlDbType.VarChar);
        dp.AddParam("@UpdateTimestamp", model.UpdateTimestamp, SqlDbType.DateTime);
        dp.AddParam("@VoidUserId", model.VoidUserId, SqlDbType.VarChar);
        dp.AddParam("@VoidTimestamp", model.VoidTimestamp, SqlDbType.DateTime);

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
               EstimasiDurasi = @EstimasiDurasi,
               PreferedDate = @PreferedDate,
               SpecialEquipment = @SpecialEquipment,
               OrderOpState = @OrderOpState,
               CreateUserId = @CreateUserId,
               CreateTimestamp = @CreateTimestamp,
               UpdateUserId = @UpdateUserId,
               UpdateTimestamp = @UpdateTimestamp,
               VoidUserId = @VoidUserId,
               VoidTimestamp = @VoidTimestamp    
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
        dp.AddParam("@EstimasiDurasi", model.EstimasiDurasi, SqlDbType.Int);
        dp.AddParam("@PreferedDate", model.PreferedDate, SqlDbType.DateTime);
        dp.AddParam("@SpecialEquipment", model.SpecialEquipment, SqlDbType.VarChar);
        dp.AddParam("@OrderOpState", model.OrderOpState, SqlDbType.Int);
        dp.AddParam("@CreateUserId", model.CreateUserId, SqlDbType.VarChar);
        dp.AddParam("@CreateTimestamp", model.CreateTimestamp, SqlDbType.DateTime);
        dp.AddParam("@UpdateUserId", model.UpdateUserId, SqlDbType.VarChar);
        dp.AddParam("@UpdateTimestamp", model.UpdateTimestamp, SqlDbType.DateTime);
        dp.AddParam("@VoidUserId", model.VoidUserId, SqlDbType.VarChar);
        dp.AddParam("@VoidTimestamp", model.VoidTimestamp, SqlDbType.DateTime);

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
               aa.DokterId, aa.EstimasiDurasi, aa.PreferedDate,
               aa.SpecialEquipment, aa.OrderOpState, 
               aa.CreateUserId, aa.CreateTimestamp, 
               aa.UpdateUserId, aa.UpdateTimestamp, 
               aa.VoidUserId, aa.VoidTimestamp,
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
               aa.DokterId, aa.EstimasiDurasi, aa.PreferedDate,
               aa.SpecialEquipment, aa.OrderOpState, 
               aa.CreateUserId, aa.CreateTimestamp, 
               aa.UpdateUserId, aa.UpdateTimestamp, 
               aa.VoidUserId, aa.VoidTimestamp,
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
