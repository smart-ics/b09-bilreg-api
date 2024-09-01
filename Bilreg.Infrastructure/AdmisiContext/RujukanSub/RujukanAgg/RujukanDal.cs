﻿using Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuna.Lib.DataAccessHelper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.RujukanAgg;
public class RujukanDal : IRujukanDal
{
    private readonly DatabaseOptions _opt;

    public RujukanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RujukanModel model)
    {
        const string sql = @"
            INSERT INTO ta_rujukan (
                fs_kd_rujukan, fs_nm_rujukan, fb_aktif, fs_alm_rujukan, 
                fs_alm2_rujukan, fs_kota_rujukan, fs_tlp_rujukan, 
                fs_kd_rujukan_tipe, fs_nm_rujukan_tipe, fs_kd_kelas, 
                fs_nm_kelas, fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk
            )
            VALUES (
                @fs_kd_rujukan, @fs_nm_rujukan, @fb_aktif, @fs_alm_rujukan, 
                @fs_alm2_rujukan, @fs_kota_rujukan, @fs_tlp_rujukan, 
                @fs_kd_rujukan_tipe, @fs_nm_rujukan_tipe, @fs_kd_kelas, 
                @fs_nm_kelas, @fs_kd_cara_masuk_dk, @fs_nm_cara_masuk_dk
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", model.RujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan", model.RujukanName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_alm_rujukan", model.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_rujukan", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_rujukan", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_rujukan", model.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rujukan_tipe", model.TipeRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan_tipe", model.TipeRujukanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", model.KelasRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", model.KelasRujukanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", model.CaraMasukDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_cara_masuk_dk", model.CaraMasukDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RujukanModel model)
    {
        const string sql = @"
            UPDATE ta_rujukan
            SET 
                fs_nm_rujukan = @fs_nm_rujukan,
                fb_aktif = @fb_aktif,
                fs_alm_rujukan = @fs_alm_rujukan,
                fs_alm2_rujukan = @fs_alm2_rujukan,
                fs_kota_rujukan = @fs_kota_rujukan,
                fs_tlp_rujukan = @fs_tlp_rujukan,
                fs_kd_rujukan_tipe = @fs_kd_rujukan_tipe,
                fs_nm_rujukan_tipe = @fs_nm_rujukan_tipe,
                fs_kd_kelas = @fs_kd_kelas,
                fs_nm_kelas = @fs_nm_kelas,
                fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk,
                fs_nm_cara_masuk_dk = @fs_nm_cara_masuk_dk
            WHERE fs_kd_rujukan = @fs_kd_rujukan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", model.RujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan", model.RujukanName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_alm_rujukan", model.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_rujukan", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_rujukan", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_rujukan", model.NoTelp, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rujukan_tipe", model.TipeRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan_tipe", model.TipeRujukanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", model.KelasRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", model.KelasRujukanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", model.CaraMasukDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_cara_masuk_dk", model.CaraMasukDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RujukanModel GetData(IRujukanKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_rujukan, aa.fs_nm_rujukan, aa.fb_aktif, aa.fs_alm_rujukan,
                aa.fs_alm2_rujukan, aa.fs_kota_rujukan, aa.fs_tlp_rujukan,
                aa.fs_kd_rujukan_tipe, ISNULL(bb.fs_nm_rujukan_tipe, '') fs_nm_rujukan_tipe,
                aa.fs_kd_kelas, ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas, 
                ISNULL(cc.fn_nilai, 0) fn_nilai,
                aa.fs_kd_cara_masuk_dk, ISNULL(dd.fs_nm_cara_masuk_dk, '') fs_nm_cara_masuk_dk
            FROM ta_rujukan aa
            LEFT JOIN ta_rujukan_tipe bb ON aa.fs_kd_rujukan_tipe = bb.fs_kd_rujukan_tipe
            LEFT JOIN tc_kelas_rs cc ON aa.fs_kd_kelas = cc.fs_kd_kelas
            LEFT JOIN ta_cara_masuk_dk dd ON aa.fs_kd_cara_masuk_dk = dd.fs_kd_cara_masuk_dk
            WHERE aa.fs_kd_rujukan = @fs_kd_rujukan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", key.RujukanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<RujukanDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<RujukanModel> ListData()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_rujukan, aa.fs_nm_rujukan, aa.fb_aktif, aa.fs_alm_rujukan,
                aa.fs_alm2_rujukan, aa.fs_kota_rujukan, aa.fs_tlp_rujukan,
                aa.fs_kd_rujukan_tipe, ISNULL(bb.fs_nm_rujukan_tipe, '') fs_nm_rujukan_tipe,
                aa.fs_kd_kelas, ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas,
                ISNULL(cc.fn_nilai, 0) fn_nilai,
                aa.fs_kd_cara_masuk_dk, ISNULL(dd.fs_nm_cara_masuk_dk, '') fs_nm_cara_masuk_dk
            FROM ta_rujukan aa
            LEFT JOIN ta_rujukan_tipe bb ON aa.fs_kd_rujukan_tipe = bb.fs_kd_rujukan_tipe
            LEFT JOIN tc_kelas_rs cc ON aa.fs_kd_kelas = cc.fs_kd_kelas
            LEFT JOIN ta_cara_masuk_dk dd ON aa.fs_kd_cara_masuk_dk = dd.fs_kd_cara_masuk_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<RujukanDto>(sql);
        return result?.Select(x => x.ToModel())!;
    }
}


public class RujukanDto
{
    public string fs_kd_rujukan { get; set; }
    public string fs_nm_rujukan { get; set; }
    public string fs_alm_rujukan { get; set; }
    public string fs_alm2_rujukan { get; set; }
    public string fs_kota_rujukan { get; set; }
    public string fs_tlp_rujukan { get; set; }
    public bool fb_aktif { get; set; }
    public string fs_kd_rujukan_tipe { get; set; }
    public string fs_nm_rujukan_tipe { get; set; }
    public string fs_kd_kelas { get; set; }
    public string fs_nm_kelas { get; set; } 
    public decimal nilai { get; set; } 
    public string fs_kd_cara_masuk_dk { get; set; }
    public string fs_nm_cara_masuk_dk { get; set; }

    public RujukanModel ToModel()
    {
        var rujukan = RujukanModel.Create(fs_kd_rujukan, fs_nm_rujukan);

        rujukan.SetAlamat(fs_alm_rujukan, fs_alm2_rujukan, fs_kota_rujukan);
        rujukan.SetNoTelp(fs_tlp_rujukan);

        var tipeRujukan = TipeRujukanModel.Create(fs_kd_rujukan_tipe, fs_nm_rujukan_tipe);
        rujukan.SetTipeRujukan(tipeRujukan);

        var kelasRujukan = KelasRujukanModel.Create(fs_kd_kelas, fs_nm_kelas, nilai);
        rujukan.SetKelasRujukan(kelasRujukan);

        var caraMasukDk = CaraMasukDkModel.Create(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk);
        rujukan.SetCaraMasukDk(caraMasukDk);

        if (fb_aktif)
            rujukan.SetAktif();
        else
            rujukan.UnSetAktif(); 

        return rujukan;
    }
}
public class RujukanDalTest
{
    private readonly RujukanDal _sut;

    public RujukanDalTest()
    {
        _sut = new RujukanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = RujukanModel.Create("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = RujukanModel.Create("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = RujukanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = RujukanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}

