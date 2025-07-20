using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienDal :
    IInsert<PasienDto>,
    IUpdate<PasienDto>,
    IDelete<IPasienKey>,
    IGetDataMayBe<PasienDto, IPasienKey>,
    IListDataMayBe<PasienDto, DateTime>
{
}

public class PasienDal : IPasienDal
{
    private readonly DatabaseOptions _opt;

    public PasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }


    public void Insert(PasienDto model)
    {
        const string sql = @"
            INSERT INTO tc_mr(
                fs_mr, fs_nm_pasien, fd_tgl_lahir, fs_jns_kelamin,
                fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, fs_gol_darah,
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, fs_kota_pasien, 
                fs_kd_pos_pasien, fs_kd_kelurahan, fs_jenis_id, fs_kd_identitas,
                fs_no_kk, fs_email, fs_tlp_pasien, fs_no_hp,
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga, fs_alm1_keluarga,
                fs_alm2_keluarga, fs_kota_keluarga, fs_kd_pos_keluarga,
                fs_kd_status_kawin_dk, fs_kd_agama, fs_kd_suku, fs_kd_pekerjaan_dk,
                fs_kd_pendidikan_dk, fd_tgl_mr, fb_aktif
            ) VALUES (
                @fs_mr, @fs_nm_pasien, @fd_tgl_lahir, @fs_jns_kelamin,
                @fs_nm_alias, @fs_temp_lahir, @fs_nm_ibu_kandung, @fs_gol_darah,
                @fs_alm_pasien, @fs_alm2_pasien, @fs_alm3_pasien, @fs_kota_pasien,
                @fs_kd_pos_pasien, @fs_kd_kelurahan, @fs_jenis_id, @fs_kd_identitas,
                @fs_no_kk, @fs_email, @fs_tlp_pasien, @fs_no_hp,
                @fs_nm_keluarga, @fs_hub_keluarga, @fs_telp_keluarga, @fs_alm1_keluarga,
                @fs_alm2_keluarga, @fs_kota_keluarga, @fs_kd_pos_keluarga,
                @fs_kd_status_kawin_dk, @fs_kd_agama, @fs_kd_suku, @fs_kd_pekerjaan_dk,
                @fs_kd_pendidikan_dk, @fd_tgl_mr, @fb_aktif
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.fd_tgl_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.fs_jns_kelamin, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_alias", model.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.fs_temp_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.fs_nm_ibu_kandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.fs_gol_darah, SqlDbType.VarChar);
        
        dp.AddParam("@fs_alm_pasien", model.fs_alm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.fs_alm2_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.fs_alm3_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.fs_kota_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.fs_kd_pos_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.fs_kd_kelurahan, SqlDbType.VarChar);
        
        dp.AddParam("@fs_jenis_id", model.fs_jenis_id, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.fs_kd_identitas, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.fs_no_kk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.fs_email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.fs_tlp_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.fs_no_hp, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_keluarga", model.fs_nm_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.fs_hub_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.fs_telp_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.fs_alm1_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.fs_alm2_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.fs_kota_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.fs_kd_pos_keluarga, SqlDbType.VarChar);
        
        dp.AddParam("@fs_kd_status_kawin_dk", model.fs_kd_status_kawin_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.fs_kd_agama, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.fs_kd_suku, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.fs_kd_pekerjaan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.fs_kd_pendidikan_dk, SqlDbType.VarChar);
        
        dp.AddParam("@fd_tgl_mr", model.fd_tgl_mr, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienDto model)
    {
        const string sql = @"
            UPDATE tc_mr
            SET 
                fs_nm_pasien = @fs_nm_pasien,
                fd_tgl_lahir = @fd_tgl_lahir,
                fs_jns_kelamin = @fs_jns_kelamin,
                fs_nm_alias = @fs_nm_alias,
                fs_temp_lahir = @fs_temp_lahir,
                fs_nm_ibu_kandung = @fs_nm_ibu_kandung,
                fs_gol_darah = @fs_gol_darah,
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
                fs_kd_pos_keluarga = @fs_kd_pos_keluarga,
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk,
                fs_kd_agama = @fs_kd_agama,
                fs_kd_suku = @fs_kd_suku,
                fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk,
                fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk,
                fd_tgl_mr = @fd_tgl_mr,
                fb_aktif = @fb_aktif
            WHERE fs_mr = @fs_mr";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.fd_tgl_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.fs_jns_kelamin, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_alias", model.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.fs_temp_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.fs_nm_ibu_kandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.fs_gol_darah, SqlDbType.VarChar);
        
        dp.AddParam("@fs_alm_pasien", model.fs_alm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.fs_alm2_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.fs_alm3_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.fs_kota_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.fs_kd_pos_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.fs_kd_kelurahan, SqlDbType.VarChar);
        
        dp.AddParam("@fs_jenis_id", model.fs_jenis_id, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.fs_kd_identitas, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.fs_no_kk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.fs_email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.fs_tlp_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.fs_no_hp, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_keluarga", model.fs_nm_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.fs_hub_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.fs_telp_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.fs_alm1_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.fs_alm2_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.fs_kota_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.fs_kd_pos_keluarga, SqlDbType.VarChar);
        
        dp.AddParam("@fs_kd_status_kawin_dk", model.fs_kd_status_kawin_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.fs_kd_agama, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.fs_kd_suku, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.fs_kd_pekerjaan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.fs_kd_pendidikan_dk, SqlDbType.VarChar);
        
        dp.AddParam("@fd_tgl_mr", model.fd_tgl_mr, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IPasienKey key)
    {
        const string sql = @"
            DELETE FROM tc_mr
            WHERE fs_mr = @fs_mr";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PasienDto> GetData(IPasienKey key)
    {
        const string sql = @"
            SELECT 
                fs_mr, fs_nm_pasien, fd_tgl_lahir, fs_jns_kelamin,
                fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, fs_gol_darah,
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, fs_kota_pasien,
                fs_kd_pos_pasien, fs_kd_kelurahan, fs_jenis_id, fs_kd_identitas,
                fs_no_kk, fs_email, fs_tlp_pasien, fs_no_hp,
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga, fs_alm1_keluarga,
                fs_alm2_keluarga, fs_kota_keluarga, fs_kd_pos_keluarga,
                fs_kd_status_kawin_dk, fs_kd_agama, fs_kd_suku, fs_kd_pekerjaan_dk,
                fs_kd_pendidikan_dk, fd_tgl_mr, fb_aktif
            FROM tc_mr
            WHERE fs_mr = @fs_mr";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PasienDto>(sql, dp));
    }

    public MayBe<IEnumerable<PasienDto>> ListData(DateTime filter)
    {
        const string sql = @"
            SELECT 
                fs_mr, fs_nm_pasien, fd_tgl_lahir, fs_jns_kelamin,
                fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, fs_gol_darah,
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, fs_kota_pasien,
                fs_kd_pos_pasien, fs_kd_kelurahan, fs_jenis_id, fs_kd_identitas,
                fs_no_kk, fs_email, fs_tlp_pasien, fs_no_hp,
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga, fs_alm1_keluarga,
                fs_alm2_keluarga, fs_kota_keluarga, fs_kd_pos_keluarga,
                fs_kd_status_kawin_dk, fs_kd_agama, fs_kd_suku, fs_kd_pekerjaan_dk,
                fs_kd_pendidikan_dk, fd_tgl_mr, fb_aktif
            FROM tc_mr
            WHERE fd_tgl_lahir = @fd_tgl_lahir";

        var dp = new DynamicParameters();
        dp.AddParam("@fd_tgl_lahir", filter.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PasienDto>(sql, dp));
    }
}

public class PasienDalTest
{
    private readonly PasienDal _sut;

    public PasienDalTest()
    {
        _sut = new PasienDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InserTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PasienDto(PasienModel.Default));
    }
    
    [Fact]
    public void UT2_InserTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PasienDto(PasienModel.Default));
    }
    
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PasienModel.Key("A"));
    }
    
    private PasienDto PasienDtoFaker() 
        => new PasienDto
        {
            fs_mr = "A",
            fs_nm_pasien = "B",
            fd_tgl_lahir = DateTime.Now.ToString("yyyy-MM-dd"),
            fs_jns_kelamin = "C",
            fs_nm_alias = "D",
            fs_temp_lahir = "E",
            fs_nm_ibu_kandung = "F",
            fs_gol_darah = "G",
            fs_alm_pasien = "H",
            fs_alm2_pasien = "I",
            fs_alm3_pasien = "J",
            fs_kota_pasien = "K",
            fs_kd_pos_pasien = "L",
            fs_kd_kelurahan = "M",
            fs_jenis_id = "N",
            fs_kd_identitas = "O",
            fs_no_kk = "P",
            fs_email = "Q",
            fs_tlp_pasien = "R",
            fs_no_hp = "S",
            fs_nm_keluarga = "T",
            fs_hub_keluarga = "U",
            fs_telp_keluarga = "V",     
            fs_alm1_keluarga = "W",
            fs_alm2_keluarga = "X",
            fs_kota_keluarga = "Y",
            fs_kd_pos_keluarga = "Z",
            fs_kd_status_kawin_dk = "1",
            fs_kd_agama = "2",
            fs_kd_suku = "3",
            fs_kd_pekerjaan_dk = "4",
            fs_kd_pendidikan_dk = "5",
            fd_tgl_mr = DateTime.Now.ToString("yyyy-MM-dd"),
            fb_aktif = true
        };
        
    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = PasienDtoFaker();
        _sut.Insert(expected);
        var actual = _sut.GetData(PasienModel.Key(expected.fs_mr)).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = PasienDtoFaker();
        _sut.Insert(expected);
        var actual = _sut.ListData(DateTime.Now).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}

