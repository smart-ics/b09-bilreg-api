using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanDal: IKelurahanDal
{
    private readonly DatabaseOptions _opt;

    public KelurahanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelurahanModel model)
    {
        const string sql = @"
            INSERT INTO ta_kelurahan (fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_kecamatan, fs_kd_pos)
            VALUES (@fs_kd_kelurahan, @fs_nm_kelurahan, @fs_kd_kecamatan, @fs_kd_pos)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos", model.KodePos, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelurahanModel model)
    {
        const string sql = @"
            UPDATE ta_kelurahan
            SET fs_nm_kelurahan = @fs_nm_kelurahan,
                fs_kd_kecamatan = @fs_kd_kecamatan,
                fs_kd_pos = @fs_kd_pos
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos", model.KodePos, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelurahanKey key)
    {
        const string sql = @"
            DELETE FROM ta_kelurahan
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KelurahanModel GetData(IKelurahanKey key)
    {
        const string sql = @"
            SELECT kel.fs_kd_kelurahan, kel.fs_nm_kelurahan, kel.fs_kd_pos, kel.fs_kd_kecamatan,
                ISNULL(kec.fs_nm_kecamatan, '') fs_nm_kecamatan, kec.fs_kd_kabupaten,
                ISNULL(kab.fs_nm_kabupaten, '') fs_nm_kabupaten, kab.fs_kd_propinsi,
                ISNULL(prop.fs_nm_propinsi, '') fs_nm_propinsi
            FROM ta_kelurahan kel
                LEFT JOIN ta_kecamatan kec ON kel.fs_kd_kecamatan = kec.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten kab ON kec.fs_kd_kabupaten = kab.fs_kd_kabupaten
                LEFT JOIN ta_propinsi prop ON kab.fs_kd_propinsi = prop.fs_kd_propinsi
            WHERE kel.fs_kd_kelurahan = @fs_kd_kelurahan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KelurahanDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<KelurahanModel> ListData(IKecamatanKey filter)
    {
        const string sql = @"
            SELECT kel.fs_kd_kelurahan, kel.fs_nm_kelurahan, kel.fs_kd_pos, kel.fs_kd_kecamatan,
                ISNULL(kec.fs_nm_kecamatan, '') fs_nm_kecamatan, kec.fs_kd_kabupaten,
                ISNULL(kab.fs_nm_kabupaten, '') fs_nm_kabupaten, kab.fs_kd_propinsi,
                ISNULL(prop.fs_nm_propinsi, '') fs_nm_propinsi
            FROM ta_kelurahan kel
                LEFT JOIN ta_kecamatan kec ON kel.fs_kd_kecamatan = kec.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten kab ON kec.fs_kd_kabupaten = kab.fs_kd_kabupaten
                LEFT JOIN ta_propinsi prop ON kab.fs_kd_propinsi = prop.fs_kd_propinsi
            WHERE kel.fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", filter.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KelurahanDto>(sql, dp);
        return result?.Select(x => x.ToModel())!;
    }

    public IEnumerable<KelurahanModel> ListData(string filter)
    {
        const string sql = @"
            SELECT kel.fs_kd_kelurahan, kel.fs_nm_kelurahan, kel.fs_kd_pos, kel.fs_kd_kecamatan,
                ISNULL(kec.fs_nm_kecamatan, '') fs_nm_kecamatan, kec.fs_kd_kabupaten,
                ISNULL(kab.fs_nm_kabupaten, '') fs_nm_kabupaten, kab.fs_kd_propinsi,
                ISNULL(prop.fs_nm_propinsi, '') fs_nm_propinsi
            FROM ta_kelurahan kel
                LEFT JOIN ta_kecamatan kec ON kel.fs_kd_kecamatan = kec.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten kab ON kec.fs_kd_kabupaten = kab.fs_kd_kabupaten
                LEFT JOIN ta_propinsi prop ON kab.fs_kd_propinsi = prop.fs_kd_propinsi
            WHERE kel.fs_nm_kelurahan LIKE '%' + @keyword + '%'";

        var dp = new DynamicParameters();
        dp.AddParam("@keyword", filter, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KelurahanDto>(sql, dp);
        return result?.Select(x => x.ToModel())!;
    }
}

public class KelurahanDalTest
{
    private readonly KelurahanDal _sut;
    private readonly KecamatanDal _kecamatanDal;
    private readonly KabupatenDal _kabupatenDal;

    public KelurahanDalTest()
    {
        _sut = new KelurahanDal(ConnStringHelper.GetTestEnv());
        _kecamatanDal = new KecamatanDal(ConnStringHelper.GetTestEnv());
        _kabupatenDal = new KabupatenDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var kelurahan = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "I");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        kelurahan.Set(kecamatan);
        _sut.Insert(kelurahan);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var kelurahan = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "I");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        kelurahan.Set(kecamatan);
        _sut.Update(kelurahan);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var kelurahan = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "I");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        kelurahan.Set(kecamatan);
        _sut.Delete(kelurahan);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        expected.Set(kecamatan);
        
        _kecamatanDal.Insert(kecamatan);
        _kabupatenDal.Insert(kabupaten);
        _sut.Insert(expected);
        
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        expected.Set(kecamatan);
        
        _kecamatanDal.Insert(kecamatan);
        _kabupatenDal.Insert(kabupaten);
        _sut.Insert(expected);
        
        var actual = _sut.ListData(kecamatan);
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
    
    [Fact]
    public void SearchDataTest()
    {
        using var trans = TransHelper.NewScope();
        const string keyword = "B";
        var expected = KelurahanModel.Create("A", "ABC", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        var propinsi = PropinsiModel.Create("H", "");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        expected.Set(kecamatan);
        
        _kecamatanDal.Insert(kecamatan);
        _kabupatenDal.Insert(kabupaten);
        _sut.Insert(expected);
        
        var actual = _sut.ListData(keyword);
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
} 