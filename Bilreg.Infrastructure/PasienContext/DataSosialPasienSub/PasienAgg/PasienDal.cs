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
                fs_email, fs_tlp_pasien, fs_no_hp)
            VALUES(
                @fs_mr, @fs_nm_pasien, @fs_nm_alias, @fs_temp_lahir, @fd_tgl_lahir, 
                @fs_jns_kelamin, @fd_tgl_mr, @fs_nm_ibu_kandung, @fs_gol_darah, 
                @fs_kd_status_kawin_dk, @fs_kd_agama, @fs_kd_suku, 
                @fs_kd_pekerjaan_dk, @fs_kd_pendidikan_dk, 
                @fs_alm_pasien, @fs_alm2_pasien, @fs_alm3_pasien, 
                @fs_kota_pasien, @fs_kd_pos_pasien, @fs_kd_kelurahan, 
                @fs_jenis_id, @fs_kd_identitas, @fs_no_kk, 
                @fs_email, @fs_tlp_pasien, @fs_no_hp) ";
        
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
                fs_no_hp = @fs_no_hp
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
        const string sql = @"
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fs_nm_alias, aa.fs_temp_lahir, aa.fd_tgl_lahir, 
                aa.fs_jns_kelamin, aa.fd_tgl_mr, aa.fs_nm_ibu_kandung, aa.fs_gol_darah, 
                aa.fs_kd_status_kawin_dk, aa.fs_kd_agama, aa.fs_kd_suku, 
                aa.fs_kd_pekerjaan_dk, aa.fs_kd_pendidikan_dk,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, 
                aa.fs_kota_pasien, aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
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
                LEFT JOIN ta_propinsi jj ON ii.fs_kd_propinsi = jj.fs_kd_propinsi
            WHERE fs_mr = @fs_mr ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienDto>(sql, dp);
    }

    public IEnumerable<PasienModel> ListData(DateTime filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fs_nm_alias, aa.fs_temp_lahir, aa.fd_tgl_lahir, 
                aa.fs_jns_kelamin, aa.fd_tgl_mr, aa.fs_nm_ibu_kandung, aa.fs_gol_darah, 
                aa.fs_kd_status_kawin_dk, aa.fs_kd_agama, aa.fs_kd_suku, 
                aa.fs_kd_pekerjaan_dk, aa.fs_kd_pendidikan_dk,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, 
                aa.fs_kota_pasien, aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
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
                LEFT JOIN ta_propinsi jj ON ii.fs_kd_propinsi = jj.fs_kd_propinsi
            WHERE aa.fd_tgl_lahir = @fd_tgl_lahir ";

        var dp = new DynamicParameters();
        dp.AddParam("@fd_tgl_lahir", filter.Date.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienDto>(sql, dp);
    }
}

internal class PasienDto() : PasienModel(string.Empty, string.Empty)
{
    public string fs_mr { get => PasienId; set => PasienId = value; }
    public string fs_nm_pasien {get => PasienName; set => PasienName = value;}
    public string fs_nm_alias { get => NickName; set => NickName = value;}

    public string fs_temp_lahir {get => TempatLahir; set => TempatLahir = value;}
    public string fd_tgl_lahir {get => TglLahir.ToString("yyyy-MM-dd"); set => TglLahir = value.ToDate(DateFormatEnum.YMD);}
    public string fs_jns_kelamin {get => Gender; set => Gender = value;}
    public string fd_tgl_mr {get => TglMedrec.ToString("yyyy-MM-dd"); set => TglMedrec = value.ToDate(DateFormatEnum.YMD);}
    public string fs_nm_ibu_kandung {get => IbuKandung; set => IbuKandung = value;}
    public string fs_gol_darah {get => GolDarah; set => GolDarah = value;}
    
    public string fs_kd_status_kawin_dk {get => StatusNikahId; set => StatusNikahId = value;}
    public string fs_nm_status_kawin_dk {get => StatusNikahName; set => StatusNikahName = value;}
    public string fs_kd_agama {get => AgamaId; set => AgamaId = value;}
    public string fs_nm_agama {get => AgamaName; set => AgamaName = value;}
    public string fs_kd_suku {get => SukuId; set => SukuId = value;}
    public string fs_nm_suku {get => SukuName; set => SukuName = value;}
    public string fs_kd_pekerjaan_dk {get =>PekerjaanDkId; set => PekerjaanDkId = value;}
    public string fs_nm_pekerjaan_dk {get => PekerjaanDkName; set => PekerjaanDkName = value;}
    public string fs_kd_pendidikan_dk {get => PendidikanDkId; set => PendidikanDkId = value;}
    public string fs_nm_pendidikan_dk {get => PendidikanDkName; set => PendidikanDkName = value;}
    
    public string fs_alm_pasien {get => Alamat; set => Alamat = value;}
    public string fs_alm2_pasien {get => Alamat2; set => Alamat2 = value;}
    public string fs_alm3_pasien {get => Alamat3; set => Alamat3 = value;}
    public string fs_kota_pasien {get => Kota; set => Kota = value;}
    public string fs_kd_pos_pasien {get => KodePos; set => KodePos = value;}
    public string fs_kd_kelurahan {get => KelurahanId; set => KelurahanId = value;}
    public string fs_nm_kelurahan {get => KelurahanName; set => KelurahanName = value;}
    public string fs_nm_kecamatan {get => KecamatanName; set => KecamatanName = value;}
    public string fs_nm_kabupaten {get => KabupatenName; set => KabupatenName = value;}
    public string fs_nm_propinsi {get => PropinsiName; set => PropinsiName = value;}
    public string fs_jenis_id {get => JenisId; set => JenisId = value;}
    public string fs_kd_identitas {get => NomorId; set => NomorId = value;}
    public string fs_no_kk {get => NomorKk; set => NomorKk = value;}
    
    public string fs_email {get => Email; set => Email = value;}
    public string fs_tlp_pasien {get => NoTelp; set => NoTelp = value;}
    public string fs_no_hp {get => NoHp; set => NoHp = value;}
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
        fs_kd_agama = "K",
        fs_kd_suku = "L",
        fs_kd_pekerjaan_dk = "M",
        fs_kd_pendidikan_dk = "N",
        fs_alm_pasien = "O",
        fs_alm2_pasien = "P",
        fs_alm3_pasien = "Q",
        fs_kota_pasien = "R",
        fs_kd_pos_pasien = "S",
        fs_kd_kelurahan = "T",
        fs_jenis_id = "U",
        fs_kd_identitas = "V",
        fs_no_kk = "W",
        fs_email = "X",
        fs_tlp_pasien = "Y",
        fs_no_hp = "Z"
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