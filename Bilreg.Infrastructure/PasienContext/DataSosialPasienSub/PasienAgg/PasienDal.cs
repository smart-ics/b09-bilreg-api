using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienDal : IPasienDal
{
    private readonly DatabaseOptions _opt;

    public PasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienModel model)
    {
        const string sql = @"
            INSERT INTO tc_mr(
                fs_mr, fs_nm_pasien, fs_nm_alias, fs_temp_lahir, fd_tgl_lahir, 
                fs_jns_kelamin, fd_tgl_mr, fs_nm_ibu_kandung, fs_gol_darah, 
                fs_kd_status_kawin_dk, fs_kd_agama, fs_kd_suku, 
                fs_kd_pekerjaan_dk, fs_kd_pendidikan_dk, 
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, 
                fs_kota_pasien, fs_kd_pos_pasien, fs_kd_kelurahan, 
                fs_jenis_id, fs_kd_identitas, fs_no_kk, 
                fs_email, fs_tlp_pasien, fs_no_hp,
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga,
                fs_alm1_keluarga, fs_alm2_keluarga, fs_kota_keluarga,
                fs_kd_pos_keluarga)
            VALUES(
                @fs_mr, @fs_nm_pasien, @fs_nm_alias, @fs_temp_lahir, @fd_tgl_lahir, 
                @fs_jns_kelamin, @fd_tgl_mr, @fs_nm_ibu_kandung, @fs_gol_darah, 
                @fs_kd_status_kawin_dk, @fs_kd_agama, @fs_kd_suku, 
                @fs_kd_pekerjaan_dk, @fs_kd_pendidikan_dk, 
                @fs_alm_pasien, @fs_alm2_pasien, @fs_alm3_pasien, 
                @fs_kota_pasien, @fs_kd_pos_pasien, @fs_kd_kelurahan, 
                @fs_jenis_id, @fs_kd_identitas, @fs_no_kk, 
                @fs_email, @fs_tlp_pasien, @fs_no_hp,
                @fs_nm_keluarga, @fs_hub_keluarga, @fs_telp_keluarga,
                @fs_alm1_keluarga, @fs_alm2_keluarga, @fs_kota_keluarga,
                @fs_kd_pos_keluarga)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NickName, SqlDbType.VarChar);

        dp.AddParam("@fs_temp_lahir", model.TempatLahir, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.TglLahir.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.Gender, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mr", model.TglMedrec.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.IbuKandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.GolDarah, SqlDbType.VarChar);

        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusNikahId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);

        dp.AddParam("@fs_alm_pasien", model.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.Alamat3, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.KodePos, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);

        dp.AddParam("@fs_jenis_id", model.JenisId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.NomorId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.NomorKk, SqlDbType.VarChar);

        dp.AddParam("@fs_email", model.Email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.NoHp, SqlDbType.VarChar);

        dp.AddParam("@fs_nm_keluarga", model.KeluargaName, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.KeluargaRelasi, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.KeluargaNoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.KeluargaAlamat1, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.KeluargaAlamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.KeluargaKota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.KeluargaKodePos, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienModel model)
    {
        const string sql = @"
            UPDATE tc_mr
            SET fs_nm_pasien = @fs_nm_pasien,
                fs_nm_alias = @fs_nm_alias,
                fs_temp_lahir = @fs_temp_lahir,
                fd_tgl_lahir = @fd_tgl_lahir,
                fs_jns_kelamin = @fs_jns_kelamin,
                fd_tgl_mr = @fd_tgl_mr,
                fs_nm_ibu_kandung = @fs_nm_ibu_kandung,
                fs_gol_darah = @fs_gol_darah,
                
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk,
                fs_kd_agama = @fs_kd_agama,
                fs_kd_suku = @fs_kd_suku,
                fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk,
                fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk,
                
                fs_alm_pasien = @fs_alm_pasien,
                fs_alm2_pasien = @fs_alm2_pasien,
                fs_alm3_pasien = @fs_alm3_pasien,
                fs_kota_pasien = @fs_kota_pasien,
                fs_kd_pos_pasien = @fs_kd_pos_pasien,
                fs_kd_kelurahan = @fs_kd_kelurahan,
                
                fs_jenis_id = @fs_jenis_id,
                fs_kd_identitas = @fs_kd_identitas,
                fs_no_kk = @fs_no_kk,
                
                fs_email = @fs_email,
                fs_tlp_pasien = @fs_tlp_pasien,
                fs_no_hp = @fs_no_hp,
                
                fs_nm_keluarga = @fs_nm_keluarga,
                fs_hub_keluarga = @fs_hub_keluarga,
                fs_telp_keluarga = @fs_telp_keluarga,
                fs_alm1_keluarga = @fs_alm1_keluarga,
                fs_alm2_keluarga = @fs_alm2_keluarga,
                fs_kota_keluarga = @fs_kota_keluarga,
                fs_kd_pos_keluarga = @fs_kd_pos_keluarga
            WHERE 
                fs_mr = @fs_mr";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NickName, SqlDbType.VarChar);

        dp.AddParam("@fs_temp_lahir", model.TempatLahir, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.TglLahir.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.Gender, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mr", model.TglMedrec.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.IbuKandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.GolDarah, SqlDbType.VarChar);

        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusNikahId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);

        dp.AddParam("@fs_alm_pasien", model.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.Alamat3, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.KodePos, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);

        dp.AddParam("@fs_jenis_id", model.JenisId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.NomorId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.NomorKk, SqlDbType.VarChar);

        dp.AddParam("@fs_email", model.Email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.NoHp, SqlDbType.VarChar);

        dp.AddParam("@fs_nm_keluarga", model.KeluargaName, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.KeluargaRelasi, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.KeluargaNoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.KeluargaAlamat1, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.KeluargaAlamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.KeluargaKota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.KeluargaKodePos, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(PasienModel key)
    {
        const string sql = @"
            DELETE FROM tc_mr
            WHERE fs_mr = @fs_mr ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienModel GetData(IPasienKey key)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE fs_mr = @fs_mr ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienDto>(sql, dp);
    }

    public IEnumerable<PasienModel> ListData(DateTime filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fd_tgl_lahir = @fd_tgl_lahir ";

        var dp = new DynamicParameters();
        dp.AddParam("@fd_tgl_lahir", filter.Date.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienDto>(sql, dp);
    }

    public IEnumerable<PasienModel> ListData(Periode filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fd_tgl_mr BETWEEN @tgl1 AND @tgl2 ";

        var dp = new DynamicParameters();
        dp.AddParam("@tgl1", filter.Tgl1.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@tgl2", filter.Tgl2.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienDto>(sql, dp);
    }

    private static string SelectFromClause() =>
        @"
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fs_nm_alias, aa.fs_temp_lahir, aa.fd_tgl_lahir, 
                aa.fs_jns_kelamin, aa.fd_tgl_mr, aa.fs_nm_ibu_kandung, aa.fs_gol_darah, 
                aa.fs_kd_status_kawin_dk, aa.fs_kd_agama, aa.fs_kd_suku, 
                aa.fs_kd_pekerjaan_dk, aa.fs_kd_pendidikan_dk,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, 
                aa.fs_kota_pasien, aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
                aa.fs_nm_keluarga, aa.fs_hub_keluarga, aa.fs_telp_keluarga,
                aa.fs_alm1_keluarga, aa.fs_alm2_keluarga, aa.fs_kota_keluarga, aa.fs_kd_pos_keluarga,
                ISNULL(bb.fs_nm_status_kawin_dk, '') AS fs_nm_status_kawin_dk,
                ISNULL(cc.fs_nm_agama,'') AS fs_nm_agama,
                ISNULL(dd.fs_nm_suku, '') AS fs_nm_suku,
                ISNULL(ee.fs_nm_pekerjaan_dk, '') AS fs_nm_pekerjaan_dk,
                ISNULL(ff.fs_nm_pendidikan_dk, '') AS fs_nm_pendidikan_dk,
                ISNULL(gg.fs_nm_kelurahan, '') AS fs_nm_kelurahan,
                ISNULL(hh.fs_nm_kecamatan, '') AS fs_nm_kecamatan,
                ISNULL(ii.fs_nm_kabupaten, '') AS fs_nm_kabupaten,
                ISNULL(jj.fs_nm_propinsi, '') AS fs_nm_propinsi 

            FROM tc_mr aa
                LEFT JOIN ta_status_kawin_dk bb ON aa.fs_kd_status_kawin_dk = bb.fs_kd_status_kawin_dk
                LEFT JOIN ta_agama cc ON aa.fs_kd_agama = cc.fs_kd_agama
                LEFT JOIN ta_suku dd ON aa.fs_kd_suku = dd.fs_kd_suku
                LEFT JOIN ta_pekerjaan_dk ee ON aa.fs_kd_pekerjaan_dk = ee.fs_kd_pekerjaan_dk
                LEFT JOIN ta_pendidikan_dk ff ON aa.fs_kd_pendidikan_dk = ff.fs_kd_pendidikan_dk
                LEFT JOIN ta_kelurahan gg ON aa.fs_kd_kelurahan = gg.fs_kd_kelurahan
                LEFT JOIN ta_kecamatan hh ON gg.fs_kd_kecamatan = hh.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten ii ON hh.fs_kd_kabupaten = ii.fs_kd_kabupaten
                LEFT JOIN ta_propinsi jj ON ii.fs_kd_propinsi = jj.fs_kd_propinsi ";
}

public class PasienDalTest
{
    private readonly PasienDal _sut;

    public PasienDalTest()
    {
        _sut = new PasienDal(ConnStringHelper.GetTestEnv());
    }

    private static PasienDto Faker() => new PasienDto
    {
        fs_mr = "A",
        fs_nm_pasien = "B",
        fs_nm_alias = "C",
        fs_temp_lahir = "D",
        fd_tgl_lahir = "2000-01-02",
        fs_jns_kelamin = "F",
        fd_tgl_mr = "2000-01-03",
        fs_nm_ibu_kandung = "H",
        fs_gol_darah = "I",
        fs_kd_status_kawin_dk = "J",
        fs_nm_status_kawin_dk = "",
        fs_kd_agama = "K",
        fs_nm_agama = "",
        fs_kd_suku = "L", 
        fs_nm_suku = "",
        fs_kd_pekerjaan_dk = "M",
        fs_nm_pekerjaan_dk = "",
        fs_kd_pendidikan_dk = "N",
        fs_nm_pendidikan_dk = "",
        fs_alm_pasien = "O",
        fs_alm2_pasien = "P",
        fs_alm3_pasien = "Q",
        fs_kota_pasien = "R",
        fs_kd_pos_pasien = "S",
        fs_kd_kelurahan = "T",
        fs_nm_kelurahan = "",
        fs_nm_kecamatan = "",
        fs_nm_kabupaten = "",
        fs_nm_propinsi = "",
        fs_jenis_id = "U",
        fs_kd_identitas = "V",
        fs_no_kk = "W",
        fs_email = "X",
        fs_tlp_pasien = "Y",
        fs_no_hp = "Z",
        fs_nm_keluarga = "AA",
        fs_hub_keluarga = "BB",
        fs_telp_keluarga = "081xx",
        fs_alm1_keluarga = "CC",
        fs_alm2_keluarga = "DD",
        fs_kota_keluarga = "EE",
        fs_kd_pos_keluarga = "528xx",
    };
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(Faker());
    }
    
    [Fact]
    public void GetTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(Faker());
        actual.Should().BeEquivalentTo(Faker());
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new DateTime(2000,1,2));
        actual.Should().ContainEquivalentOf(Faker());
    }
}