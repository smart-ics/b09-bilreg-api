using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanDal: IKecamatanDal
{
    private readonly DatabaseOptions _opt;

    public KecamatanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(KecamatanModel model)
    {
        const string sql = @"
            INSERT INTO ta_kecamatan (fs_kd_kecamatan, fs_nm_kecamatan, fs_kd_kabupaten) 
            VALUES (@fs_kd_kecamatan, @fs_nm_kecamatan, @fs_kd_kabupaten)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kecamatan", model.KecamatanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kabupaten", model.KabupatenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KecamatanModel model)
    {
        const string sql = @"
            UPDATE ta_kecamatan
            SET fs_nm_kecamatan = @fs_nm_kecamatan,
                fs_kd_kabupaten = @fs_kd_kabupaten
            WHERE fs_kd_kecamatan = @fs_kd_kecamatan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kecamatan", model.KecamatanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kabupaten", model.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKecamatanKey key)
    {
        const string sql = @"
            DELETE FROM ta_kecamatan
            WHERE fs_kd_kecamatan = @fs_kd_kecamatan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", key.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KecamatanModel GetData(IKecamatanKey key)
    {
        const string sql = @"
            SELECT kec.fs_kd_kecamatan, kec.fs_nm_kecamatan, kec.fs_kd_kabupaten,
                ISNULL(kab.fs_nm_kabupaten, '') fs_nm_kabupaten, kab.fs_kd_propinsi,
                ISNULL(prop.fs_nm_propinsi, '') fs_nm_propinsi
            FROM ta_kecamatan kec
                LEFT JOIN ta_kabupaten kab ON kec.fs_kd_kabupaten = kab.fs_kd_kabupaten
                LEFT JOIN ta_propinsi prop ON kab.fs_kd_propinsi = prop.fs_kd_propinsi
            WHERE kec.fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", key.KecamatanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KecamatanDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<KecamatanModel> ListData(IKabupatenKey filter)
    {
        const string sql = @"
            SELECT kec.fs_kd_kecamatan, kec.fs_nm_kecamatan, kec.fs_kd_kabupaten,
                ISNULL(kab.fs_nm_kabupaten, '') fs_nm_kabupaten, kab.fs_kd_propinsi,
                ISNULL(prop.fs_nm_propinsi, '') fs_nm_propinsi
            FROM ta_kecamatan kec
                LEFT JOIN ta_kabupaten kab ON kec.fs_kd_kabupaten = kab.fs_kd_kabupaten
                LEFT JOIN ta_propinsi prop ON kab.fs_kd_propinsi = prop.fs_kd_propinsi
            WHERE kec.fs_kd_kabupaten = @fs_kd_kabupaten";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", filter.KabupatenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KecamatanDto>(sql, dp);
        return result?.Select(x => x.ToModel())!;
    }
}

public class KecamatanDalTest
{
    private readonly KecamatanDal _sut;
    private readonly KabupatenDal _kabupatenDal;
    
    public KecamatanDalTest()
    {
        _sut = new KecamatanDal(ConnStringHelper.GetTestEnv());
        _kabupatenDal = new KabupatenDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var kecamatan = KecamatanModel.Create("A", "B");
        var kabupaten = KabupatenModel.Create("C", "D");
        kabupaten.Set(PropinsiModel.Create("E", "F"));
        kecamatan.Set(kabupaten);
        _sut.Insert(kecamatan);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var kecamatan = KecamatanModel.Create("A", "B");
        var kabupaten = KabupatenModel.Create("C", "D");
        kabupaten.Set(PropinsiModel.Create("E", "F"));
        kecamatan.Set(kabupaten);
        _sut.Update(kecamatan);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var kecamatan = KecamatanModel.Create("A", "B");
        var kabupaten = KabupatenModel.Create("C", "D");
        kabupaten.Set(PropinsiModel.Create("E", "F"));
        kecamatan.Set(kabupaten);
        _sut.Delete(kecamatan);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KecamatanModel.Create("A", "B");
        var kabupaten = KabupatenModel.Create("C", "D");
        var propinsi = PropinsiModel.Create("E", "");
        kabupaten.Set(propinsi);
        expected.Set(kabupaten);
        
        _kabupatenDal.Insert(kabupaten);
        _sut.Insert(expected);
        
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KecamatanModel.Create("A", "B");
        var kabupaten = KabupatenModel.Create("C", "D");
        kabupaten.Set(PropinsiModel.Create("E", "F"));
        expected.Set(kabupaten);
        
        _kabupatenDal.Insert(kabupaten);
        _sut.Insert(expected);
        
        var actual = _sut.ListData(kabupaten);
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}