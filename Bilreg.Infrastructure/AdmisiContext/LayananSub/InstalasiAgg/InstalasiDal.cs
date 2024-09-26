using Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
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
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;
using Bilreg.Infrastructure.AdmisiContext.LayananSub.InstalasiDkAgg;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.InstalasiAgg
{
    public class InstalasiDal : IInstalasiDal
    {
        private readonly DatabaseOptions _opt;

        public InstalasiDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Insert(InstalasiModel model)
        {
            const string sql = @"
            INSERT INTO ta_instalasi (fs_kd_instalasi, fs_nm_instalasi, fs_kd_instalasi_dk) 
            VALUES (@fs_kd_instalasi, @fs_nm_instalasi, @fs_kd_instalasi_dk)";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi", model.InstalasiId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_instalasi", model.InstalasiName, SqlDbType.VarChar);
            dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Update(InstalasiModel model)
        {
            const string sql = @"
            UPDATE ta_instalasi
            SET fs_nm_instalasi = @fs_nm_instalasi,
                fs_kd_instalasi_dk = @fs_kd_instalasi_dk
            WHERE fs_kd_instalasi = @fs_kd_instalasi";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi", model.InstalasiId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_instalasi", model.InstalasiName, SqlDbType.VarChar);
            dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Delete(IInstalasiKey key)
        {
            const string sql = @"
            DELETE FROM ta_instalasi
            WHERE fs_kd_instalasi = @fs_kd_instalasi";


            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi", key.InstalasiId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public InstalasiModel GetData(IInstalasiKey key)
        {
            const string sql = @"
            SELECT ins.fs_kd_instalasi,
                   ins.fs_nm_instalasi,
                   ins.fs_kd_instalasi_dk, 
                   ISNULL(indk.fs_nm_instalasi_dk, '')
                   fs_nm_instalasi_dk
            FROM ta_instalasi ins
            LEFT JOIN ta_instalasi_dk indk ON ins.fs_kd_instalasi_dk = indk.fs_kd_instalasi_dk
            WHERE ins.fs_kd_instalasi = @fs_kd_instalasi";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi", key.InstalasiId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<InstalasiDto>(sql, dp);
            return result?.ToModel()!;
        }

        public IEnumerable<InstalasiModel> ListData(IInstalasiDkKey filter)
        {
            const string sql = @"
            SELECT ins.fs_kd_instalasi, 
                   ins.fs_nm_instalasi, 
                   ins.fs_kd_instalasi_dk,
                   ISNULL(indk.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk
            FROM ta_instalasi AS ins
            LEFT JOIN ta_instalasi_dk AS indk ON ins.fs_kd_instalasi_dk = indk.fs_kd_instalasi_dk
            WHERE ins.fs_kd_instalasi_dk = @fs_kd_instalasi_dk";


            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi_dk", filter.InstalasiDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<InstalasiDto>(sql, dp);
            return result?.Select(x => x.ToModel())!;
        }

        public IEnumerable<InstalasiModel> ListData()
        {
            const string sql = @"
            SELECT ins.fs_kd_instalasi, 
                   ins.fs_nm_instalasi, 
                   ins.fs_kd_instalasi_dk,
                   ISNULL(indk.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk
            FROM ta_instalasi AS ins
            LEFT JOIN ta_instalasi_dk AS indk ON ins.fs_kd_instalasi_dk = indk.fs_kd_instalasi_dk";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<InstalasiDto>(sql);
            return result?.Select(x => x.ToModel());
        }


    }

    public class InstalasiDalTest
    {
        private readonly InstalasiDal _sut;

        public InstalasiDalTest()
        {
            _sut = new InstalasiDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            var instalasi = InstalasiModel.Create("4", "BIDANG ADMINISTRASI");
            var instalasi_dk = InstalasiDkModel.Create("4", "");
            instalasi.Set(instalasi_dk);
            _sut.Insert(instalasi);
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var instalasi = InstalasiModel.Create("A", "B");
            var instalasi_dk = InstalasiDkModel.Create("C", "");
            instalasi.Set(instalasi_dk);
            _sut.Update(instalasi);
        }

        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            var instalasi = InstalasiModel.Create("A", "B");
            var instalasi_dk = InstalasiDkModel.Create("C", "");
            instalasi.Set(instalasi_dk);

            _sut.Insert(instalasi);
            _sut.Delete(instalasi);

            var deleted = _sut.GetData(instalasi);
            deleted.Should().BeNull();
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = InstalasiModel.Create("A", "B");
            var instalasi_dk = InstalasiDkModel.Create("C", "");
            expected.Set(instalasi_dk);

            _sut.Insert(expected);
            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = InstalasiModel.Create("A", "B");
            var instalasi_dk = InstalasiDkModel.Create("C", "");
            expected.Set(instalasi_dk);

            _sut.Insert(expected);
            var actualList = _sut.ListData(instalasi_dk);

            actualList.Should().ContainSingle().Which.Should().BeEquivalentTo(expected);
        }
    }
}
