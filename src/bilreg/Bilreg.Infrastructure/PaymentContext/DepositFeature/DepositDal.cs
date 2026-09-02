using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.DepositFeature;

public interface IDepositDal :
    IInsert<DepositDto>,
    IUpdate<DepositDto>,
    IGetData<DepositDto, IDepositId>,
    IListData<DepositDto, IRegKey>
{
}

public class DepositDal : IDepositDal
{
    private readonly DatabaseOptions _opt;

    public DepositDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DepositDto dto)
    {
        const string sql = """
            INSERT INTO ta_trs_deposit(
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fd_tgl_jam_trs,
                fs_kd_petugas, fs_kd_reg, fs_kd_layanan, fs_keterangan,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_trs_gen, fs_kd_trs_deposit_khusus,
                fn_nilai_deposit, fn_total_deposit,
                crttgl, crtjam, crtusr,
                updtgl, updjam, updusr)
            VALUES(
                @TrsId, @TglTrs, @JamTrs, @TglJamTrs,
                @Petugas, @RegId, @LayananId, @Keterangan,
                @TglVoid, @JamVoid, @PetugasVoid,
                @TrsGen, @TrsDepositKhusus,
                @NilaiDeposit, @TotalDeposit,
                @CrtTgl, @CrtJam, @CrtUsr,
                @UpdTgl, @UpdJam, @UpdUsr)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(DepositDto dto)
    {
        const string sql = """
            UPDATE ta_trs_deposit
            SET fd_tgl_trs = @TglTrs,
                fs_jam_trs = @JamTrs,
                fd_tgl_jam_trs = @TglJamTrs,
                fs_kd_petugas = @Petugas,
                fs_kd_reg = @RegId,
                fs_kd_layanan = @LayananId,
                fs_keterangan = @Keterangan,
                fd_tgl_void = @TglVoid,
                fs_jam_void = @JamVoid,
                fs_kd_petugas_void = @PetugasVoid,
                fs_kd_trs_gen = @TrsGen,
                fs_kd_trs_deposit_khusus = @TrsDepositKhusus,
                fn_nilai_deposit = @NilaiDeposit,
                fn_total_deposit = @TotalDeposit,
                updtgl = @UpdTgl,
                updjam = @UpdJam,
                updusr = @UpdUsr
            WHERE fs_kd_trs = @TrsId
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public DepositDto GetData(IDepositId key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fd_tgl_jam_trs,
                aa.fs_kd_petugas, aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_keterangan,
                aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
                aa.fs_kd_trs_gen, aa.fs_kd_trs_deposit_khusus,
                aa.fn_nilai_deposit, aa.fn_total_deposit,
                aa.crttgl, aa.crtjam, aa.crtusr,
                aa.updtgl, aa.updjam, aa.updusr,
                bb.fs_mr, cc.fs_nm_pasien, dd.fs_nm_layanan
            FROM ta_trs_deposit aa
            LEFT JOIN ta_registrasi bb ON bb.fs_kd_reg = aa.fs_kd_reg
            LEFT JOIN tc_mr cc ON cc.fs_mr = bb.fs_mr
            LEFT JOIN ta_layanan dd ON dd.fs_kd_layanan = aa.fs_kd_layanan
            WHERE aa.fs_kd_trs = @TrsId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsId", key.DepositId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<DepositDto>(sql, dp);
    }

    public IEnumerable<DepositDto> ListData(IRegKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fd_tgl_jam_trs,
                aa.fs_kd_petugas, aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_keterangan,
                aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
                aa.fs_kd_trs_gen, aa.fs_kd_trs_deposit_khusus,
                aa.fn_nilai_deposit, aa.fn_total_deposit,
                aa.crttgl, aa.crtjam, aa.crtusr,
                aa.updtgl, aa.updjam, aa.updusr,
                bb.fs_mr, cc.fs_nm_pasien, dd.fs_nm_layanan
            FROM        ta_trs_deposit aa
            LEFT JOIN   ta_registrasi bb ON bb.fs_kd_reg = aa.fs_kd_reg
            LEFT JOIN   tc_mr cc ON cc.fs_mr = bb.fs_mr
            LEFT JOIN   ta_layanan dd ON dd.fs_kd_layanan = aa.fs_kd_layanan
            WHERE       aa.fs_kd_reg = @RegId
            ORDER BY aa.fd_tgl_jam_trs DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DepositDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(DepositDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TrsId", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", dto.fd_tgl_trs, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", dto.fs_jam_trs, SqlDbType.VarChar);
        dp.AddParam("@TglJamTrs", dto.fd_tgl_jam_trs, SqlDbType.VarChar);
        dp.AddParam("@Petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.fs_keterangan, SqlDbType.VarChar);
        dp.AddParam("@TglVoid", dto.fd_tgl_void, SqlDbType.VarChar);
        dp.AddParam("@JamVoid", dto.fs_jam_void, SqlDbType.VarChar);
        dp.AddParam("@PetugasVoid", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@TrsGen", dto.fs_kd_trs_gen, SqlDbType.VarChar);
        dp.AddParam("@TrsDepositKhusus", dto.fs_kd_trs_deposit_khusus, SqlDbType.VarChar);
        dp.AddParam("@NilaiDeposit", dto.fn_nilai_deposit, SqlDbType.Decimal);
        dp.AddParam("@TotalDeposit", dto.fn_total_deposit, SqlDbType.Decimal);
        dp.AddParam("@CrtTgl", dto.crttgl, SqlDbType.VarChar);
        dp.AddParam("@CrtJam", dto.crtjam, SqlDbType.VarChar);
        dp.AddParam("@CrtUsr", dto.crtusr, SqlDbType.VarChar);
        dp.AddParam("@UpdTgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@UpdJam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@UpdUsr", dto.updusr, SqlDbType.VarChar);
        return dp;
    }
}