using Bilreg.Application.AdmPasienContext.PekerjaanAgg;
using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using Bilreg.Infrastructure.AdmPasienContext.PekerjaanAgg;
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
using Xunit;

namespace Bilreg.Infrastructure.AdmPasienContext.PekerjaanAgg
{
    public class PekerjaanDal : IPekerjaanDal
    {
        private readonly DatabaseOptions _opt;

        public PekerjaanDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Insert(PekerjaanModel model)
        {
            //  QUERY
            const string sql = @"
                INSERT INTO ta_pekerjaan(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk)
                VALUES (@fs_kd_pekerjaan_dk, @fs_nm_pekerjaan_dk)";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanName, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Update(PekerjaanModel model)
        {
            //  QUERY
            const string sql = @"
                UPDATE ta_pekerjaan
                SET fs_nm_pekerjaan_dk = @fs_nm_pekerjaan_dk
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanName, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Delete(IPekerjaanKey key)
        {
            //  QUERY
            const string sql = @"
                DELETE FROM ta_pekerjaan
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanId, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public PekerjaanModel GetData(IPekerjaanKey key)
        {
            const string sql = @"
                SELECT fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk
                FROM ta_pekerjaan
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<PekerjaanDto>(sql, dp);
            return result?.ToModel();
        }

        public IEnumerable<PekerjaanModel> ListData()
        {
            const string sql = @"
                SELECT fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk
                FROM ta_pekerjaan";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<PekerjaanDto>(sql);
            return result?.Select(x => x.ToModel());
        }
    }

    public class PekerjaanDto
    {
        public string fs_kd_pekerjaan_dk { get; set; }
        public string fs_nm_pekerjaan_dk { get; set; }
        public PekerjaanModel ToModel() => PekerjaanModel.Create(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk);
    }
}

public class PekerjaanDalTest
{
    private readonly PekerjaanDal _sut;

    public PekerjaanDalTest()
    {
        _sut = new PekerjaanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(PekerjaanModel.Create("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(PekerjaanModel.Create("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PekerjaanModel.Create("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = PekerjaanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistDate_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = PekerjaanModel.Create("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<PekerjaanModel> { PekerjaanModel.Create("A", "B") };
        _sut.Insert(PekerjaanModel.Create("A", "B"));
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(expected);
    }
}