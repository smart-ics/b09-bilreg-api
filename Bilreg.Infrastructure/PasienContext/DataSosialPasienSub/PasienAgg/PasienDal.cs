using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Domain.PasienContext.SukuFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;
using SukuType = Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg.SukuType;

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
                --personal info
                fs_mr, fs_nm_pasien, fd_tgl_lahir, fs_jns_kelamin, 
                fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, fs_gol_darah, 
                -- administrative
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, 
                fs_kota_pasien, fs_kd_pos_pasien, 
                fs_kd_kelurahan, fs_jenis_id, fs_kd_identitas, fs_no_kk, 
                fs_email, fs_tlp_pasien, fs_no_hp,
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga,       
                fs_alm1_keluarga, fs_alm2_keluarga, fs_kota_keluarga,
                fs_kd_pos_keluarga,              
                -- status sosial                              
                fs_kd_status_kawin_dk, fs_kd_agama, fs_kd_suku, 
                fs_kd_pekerjaan_dk, fs_kd_pendidikan_dk, 
                -- olah berkas
                 fd_tgl_mr, fb_aktif)
            VALUES(
                @fs_mr, @fs_nm_pasien, @fd_tgl_lahir, @fs_jns_kelamin, 
                @fs_nm_alias, @fs_temp_lahir, @fs_nm_ibu_kandung, @fs_gol_darah, 
                @fs_alm_pasien, @fs_alm2_pasien, @fs_alm3_pasien, 
                @fs_kota_pasien, @fs_kd_pos_pasien, 
                @fs_kd_kelurahan, @fs_jenis_id, @fs_kd_identitas, @fs_no_kk, 
                @fs_email, @fs_tlp_pasien, @fs_no_hp,
                @fs_nm_keluarga, @fs_hub_keluarga, @fs_telp_keluarga,       
                @fs_alm1_keluarga, @fs_alm2_keluarga, @fs_kota_keluarga,
                @fs_kd_pos_keluarga,              
                @fs_kd_status_kawin_dk, @fs_kd_agama, @fs_kd_suku, 
                @fs_kd_pekerjaan_dk, @fs_kd_pendidikan_dk, 
                @fd_tgl_mr, @fb_aktif)";

        var dp = new DynamicParameters();
        //      personal info
        dp.AddParam("@fs_mr", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.TglLahir.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.Gender.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NickName, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.TempatLahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.IbuKandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.GolDarah.ToString(), SqlDbType.VarChar);
        //      administrative
        dp.AddParam("@fs_alm_pasien", model.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.Address.Alamat3, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.Address.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.Address.KodePos, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.Kelurahan.KelurahanId, SqlDbType.VarChar);
        //
        dp.AddParam("@fs_jenis_id", model.ListIdentification.JenisId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.ListIdentification.NomorId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.ListIdentification.NomorKk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.ListContact.Email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.ListContact.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.ListContact.NoHp, SqlDbType.VarChar);
        //
        dp.AddParam("@fs_nm_keluarga", model.PasienKeluarga.Name, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.PasienKeluarga.Relasi, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.PasienKeluarga.Contact.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.PasienKeluarga.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.PasienKeluarga.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.PasienKeluarga.Address.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.PasienKeluarga.Address.KodePos, SqlDbType.VarChar);
        //      status sosial
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawin.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.Agama.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.Suku.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDk.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.Pendidikan.PendidikanDkId, SqlDbType.VarChar);
        //      olah berkas
        dp.AddParam("@fd_tgl_mr", model.TglMedRec.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("fb_aktif", model.IsAktif, SqlDbType.Bit);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienModel model)
    {
        const string sql = @"
            UPDATE 
                tc_mr
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
            WHERE 
                fs_mr = @fs_mr";

        var dp = new DynamicParameters();
        //      personal info
        dp.AddParam("@fs_mr", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.TglLahir.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.Gender.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NickName, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.TempatLahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.IbuKandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.GolDarah.ToString(), SqlDbType.VarChar);
        //      administrative
        dp.AddParam("@fs_alm_pasien", model.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.Address.Alamat3, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.Address.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.Address.KodePos, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.Kelurahan.KelurahanId, SqlDbType.VarChar);
        //
        dp.AddParam("@fs_jenis_id", model.ListIdentification.JenisId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.ListIdentification.NomorId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.ListIdentification.NomorKk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.ListContact.Email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.ListContact.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.ListContact.NoHp, SqlDbType.VarChar);
        //
        dp.AddParam("@fs_nm_keluarga", model.PasienKeluarga.Name, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.PasienKeluarga.Relasi, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.PasienKeluarga.Contact.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.PasienKeluarga.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.PasienKeluarga.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.PasienKeluarga.Address.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.PasienKeluarga.Address.KodePos, SqlDbType.VarChar);
        //      status sosial
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawin.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_agama", model.Agama.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.Suku.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDk.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.Pendidikan.PendidikanDkId, SqlDbType.VarChar);
        //      olah berkas
        dp.AddParam("@fd_tgl_mr", model.TglMedRec.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("fb_aktif", model.IsAktif, SqlDbType.Bit);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(PasienModel key)
    {
        const string sql = @"
            DELETE FROM 
                tc_mr
            WHERE 
                fs_mr = @fs_mr ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GetDataResult<PasienModel> GetData2(IPasienKey key)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE fs_mr = @fs_mr ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.ReadSingle<PasienDto>(sql, dp);
        var pasien = dto?.ToModel();
        var result = new GetDataResult<PasienModel>(pasien, key.PasienId);
        return result;
    }

    public ListDataResult<PasienModel> ListData2(DateTime tglLahir)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fd_tgl_lahir = @fd_tgl_lahir ";

        var dp = new DynamicParameters();
        dp.AddParam("@fd_tgl_lahir", tglLahir.Date.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.Read<PasienDto>(sql, dp);
        var pasien = dto?.Select(x => x.ToModel());
        var result = new ListDataResult<PasienModel>(pasien);
        return result;
    }

    public ListDataResult<PasienModel> ListData2(Periode filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fd_tgl_mr BETWEEN @tgl1 AND @tgl2 ";

        var dp = new DynamicParameters();
        dp.AddParam("@tgl1", filter.Tgl1.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@tgl2", filter.Tgl2.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.Read<PasienDto>(sql, dp);
        var pasien = dto?.Select(x => x.ToModel());
        var result  = new ListDataResult<PasienModel>(pasien);
        return result;
    }

    public ListDataResult<PasienModel> ListData2(string filter)
    {
        var sql = $@"{SelectFromClause()} 
            WHERE aa.fs_nm_pasien = @fs_nm_pasien ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_nm_pasien", filter, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto =  conn.Read<PasienDto>(sql, dp);
        var pasien = dto?.Select(x => x.ToModel());
        var result = new ListDataResult<PasienModel>(pasien);
        return result;
    }
    
    private static string SelectFromClause() =>
        @"
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fd_tgl_lahir, aa.fs_jns_kelamin, 
                aa.fs_nm_alias, aa.fs_temp_lahir, aa.fs_nm_ibu_kandung, aa.fs_gol_darah, 
                --      administrative
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, 
                aa.fs_kota_pasien, aa.fs_kd_pos_pasien, 
                aa.fs_kd_kelurahan, aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
                aa.fs_nm_keluarga, aa.fs_hub_keluarga, aa.fs_telp_keluarga,       
                aa.fs_alm1_keluarga, aa.fs_alm2_keluarga, aa.fs_kota_keluarga,
                aa.fs_kd_pos_keluarga,              
                --      status sosial                              
                aa.fs_kd_status_kawin_dk, aa.fs_kd_agama, aa.fs_kd_suku, 
                aa.fs_kd_pekerjaan_dk, aa.fs_kd_pendidikan_dk, 
                --      olah berkas
                aa.fd_tgl_mr, aa.fb_aktif,
                ISNULL(bb.fs_nm_kelurahan, '') AS fs_nm_kelurahan,
                ISNULL(bb.fs_kd_kecamatan, '') AS fs_kd_kecamatan,
                ISNULL(cc.fs_nm_kecamatan, '') AS fs_nm_kecamatan,
                ISNULL(cc.fs_kd_kabupaten, '') AS fs_kd_kabupaten,
                ISNULL(dd.fs_nm_kabupaten, '') AS fs_nm_kabupaten,
                ISNULL(dd.fs_kd_propinsi, '') AS fs_kd_propinsi,
                ISNULL(ee.fs_nm_propinsi, '') AS fs_nm_propinsi, 
                ISNULL(ff.fs_nm_status_kawin_dk, '') AS fs_nm_status_kawin_dk,
                ISNULL(gg.fs_nm_agama,'') AS fs_nm_agama,
                ISNULL(hh.fs_nm_suku, '') AS fs_nm_suku,
                ISNULL(ii.fs_nm_pekerjaan_dk, '') AS fs_nm_pekerjaan_dk,
                ISNULL(jj.fs_nm_pendidikan_dk, '') AS fs_nm_pendidikan_dk

            FROM 
                tc_mr aa
                LEFT JOIN ta_kelurahan bb ON aa.fs_kd_kelurahan = bb.fs_kd_kelurahan
                LEFT JOIN ta_kecamatan cc ON bb.fs_kd_kecamatan = cc.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten dd ON cc.fs_kd_kabupaten = dd.fs_kd_kabupaten
                LEFT JOIN ta_propinsi ee ON dd.fs_kd_propinsi = ee.fs_kd_propinsi 
                LEFT JOIN ta_status_kawin_dk ff ON aa.fs_kd_status_kawin_dk = ff.fs_kd_status_kawin_dk
                LEFT JOIN ta_agama gg ON aa.fs_kd_agama = gg.fs_kd_agama
                LEFT JOIN ta_suku hh ON aa.fs_kd_suku = hh.fs_kd_suku
                LEFT JOIN ta_pekerjaan_dk ii ON aa.fs_kd_pekerjaan_dk = ii.fs_kd_pekerjaan_dk
                LEFT JOIN ta_pendidikan_dk jj ON aa.fs_kd_pendidikan_dk = jj.fs_kd_pendidikan_dk ";

}

public class PasienDalTest
{
    private readonly PasienDal _sut;

    public PasienDalTest()
    {
        _sut = new PasienDal(ConnStringHelper.GetTestEnv());
    }

    private static PasienModel Faker()
    {
        var result = new PasienModel("A", "B", new DateTime(2002,3,4), new GenderType("F"));
        result.SetPersonalInfo("C", "D", "E", new GolDarahType("A"));
        result.SetAdministrativeInfo(
            AddressType.Default,  
            KelurahanModel.Default, 
            IdentitasType.Default, 
            ContactType.Default, 
            PasienKeluargaType.Default);
        result.SetStatusSosial(
            StatusKawinDkModel.Default,
            AgamaModel.Default,
            SukuType.Default,
            PekerjaanDkModel.Default,
            PendidikanDkModel.Default
            );
        return result;
    }


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
        var actual = _sut.GetData2(Faker()).Value;
        actual.Should().BeEquivalentTo(Faker());
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData2(new DateTime(2002,3,4)).Value;
        actual.Should().ContainEquivalentOf(Faker());
    }
}