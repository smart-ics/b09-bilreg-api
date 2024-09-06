using Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public class GrupRekapCetakDal : IGrupRekapCetakDal
{
    private readonly DatabaseOptions _opt;
    public GrupRekapCetakDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GrupRekapCetakModel model)
    {
        const string sql = @"
            INSERT INTO ta_grup_rekap_cetak (fs_kd_grup_rekap_cetak, fs_nm_grup_rekap_cetak)
            VAlUES (@fs_kd_grup_rekap_cetak, @fs_nm_grup_rekap_cetak)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rekap_cetak", model.GrupRekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rekap_cetak", model.GrupRekapCetakName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql,dp);
    }

    public void Update(GrupRekapCetakModel model)
    {
        const string sql = @"
            UPDATE 
                ta_grup_rekap_cetak 
            SET 
                fs_nm_grup_rekap_cetak = @fs_nm_grup_rekap_cetak
            WHERE 
                fs_kd_grup_rekap_cetak = @fs_kd_grup_rekap_cetak";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rekap_cetak", model.GrupRekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rekap_cetak", model.GrupRekapCetakName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGrupRekapCetakKey key)
    {
        const string sql = @"
            DELETE FROM 
                ta_grup_rekap_cetak 
            WHERE 
                fs_kd_grup_rekap_cetak = @fs_kd_grup_rekap_cetak";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rekap_cetak", key.GrupRekapCetakId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GrupRekapCetakModel GetData(IGrupRekapCetakKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_grup_rekap_cetak, fs_nm_grup_rekap_cetak
            FROM 
                ta_grup_rekap_cetak 
            WHERE 
                fs_kd_grup_rekap_cetak = @fs_kd_grup_rekap_cetak";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rekap_cetak", key.GrupRekapCetakId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GrupRekapCetakDto>(sql,dp);
        return result;
    }
       
    public IEnumerable<GrupRekapCetakModel> ListData()
    {
        const string sql = @"
            SELECT 
                fs_kd_grup_rekap_cetak, fs_nm_grup_rekap_cetak
            FROM 
                ta_grup_rekap_cetak";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<GrupRekapCetakDto>(sql);
        return result;
    }
}

public class GrupRekapCetakDto() : GrupRekapCetakModel(string.Empty, string.Empty)
{  
    public string fs_kd_grup_rekap_cetak { get => GrupRekapCetakId; set => GrupRekapCetakId = value; }
    public string fs_nm_grup_rekap_cetak { get => GrupRekapCetakName; set => GrupRekapCetakName = value; }
}

public class GrupRekapCetakDalTest
{
    private readonly GrupRekapCetakDal _sut;

    public GrupRekapCetakDalTest()
    {
        _sut = new GrupRekapCetakDal(ConnStringHelper.GetTestEnv());
    }

    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupRekapCetakModel("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupRekapCetakModel("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupRekapCetakModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupRekapCetakModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupRekapCetakModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}

