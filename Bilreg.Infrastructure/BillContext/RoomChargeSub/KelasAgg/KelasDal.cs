using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using System.Data;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;
using FluentAssertions;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.KelasAgg;

public class KelasDal : IKelasDal
{
    private readonly DatabaseOptions _opt;

    public KelasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelasModel model)
    {
        const string sql = @"
            INSERT INTO ta_kelas (fs_kd_kelas, fs_nm_kelas, fb_aktif, fs_kd_kelas_dk) 
            VALUES (@fs_kd_kelas, @fs_nm_kelas, @fb_aktif, @fs_kd_kelas_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", model.KelasName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_dk", model.KelasDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelasModel model)
    {
        const string sql = @"
            UPDATE 
                ta_kelas
            SET 
                fs_nm_kelas = @fs_kd_kelas,
                fs_kd_kelas_dk = @fs_nm_kelas,
                fb_aktif = @fb_aktif
            WHERE 
                fs_kd_kelas = @fs_kd_kelas_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", model.KelasName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_dk", model.KelasDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelasKey key)
    {
        const string sql = @"
            DELETE FROM 
                ta_kelas
            WHERE 
                fs_kd_kelas = @fs_kd_kelas";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", key.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KelasModel GetData(IKelasKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kelas, aa.fs_nm_kelas, aa.fb_aktif, aa.fs_kd_kelas_dk,
                ISNULL(bb.fs_nm_kelas_dk, '') fs_nm_kelas_dk
            FROM 
                ta_kelas aa
                LEFT JOIN ta_kelas_dk bb ON aa.fs_kd_kelas_dk = bb.fs_kd_kelas_dk
            WHERE 
                aa.fs_kd_kelas = @fs_kd_kelas ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", key.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KelasDto>(sql, dp);
    }

    public IEnumerable<KelasModel> ListData()
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kelas, aa.fs_nm_kelas, aa.fb_aktif, aa.fs_kd_kelas_dk,
                ISNULL(bb.fs_nm_kelas_dk, '') fs_nm_kelas_dk
            FROM 
                ta_kelas aa
                LEFT JOIN ta_kelas_dk bb ON aa.fs_kd_kelas_dk = bb.fs_kd_kelas_dk ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KelasDto>(sql);
    }


    public class KelasDto : KelasModel
    {
        public string fs_kd_kelas { get => KelasId; set => KelasId = value; }
        public string fs_nm_kelas { get => KelasName; set => KelasName = value; }
        public bool fb_aktif { get => IsAktif; set => IsAktif = value; }
        public string fs_kd_kelas_dk { get => KelasDkId; set => KelasDkId = value; }
        public string fs_nm_kelas_dk { get => KelasDkName; set => KelasDkName = value; }

        public KelasDto() : base(string.Empty, string.Empty)
        {
        }
    }
}

public class KelasDalTest
{
    private readonly KelasDal _sut;

    public KelasDalTest()
    {
        _sut = new KelasDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var instalasi = KelasModel.Create("000", "NON KELAS");
        _sut.Insert(instalasi);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var instalasi = KelasModel.Create("101", "KELAS FLAT EARTH");
        _sut.Update(instalasi);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KelasModel.Create("101", "KELAS FLAT EARTH");
        var kelasDk = KelasDkModel.Create("A", "");
        expected.Set(kelasDk);
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KelasModel.Create("101", "KELAS FLAT EARTH");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }

}