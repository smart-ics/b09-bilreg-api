using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
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
using Nuna.Lib.TransactionHelper;
using Xunit;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using FluentAssertions;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.KelasAgg
{
    public class KelasDal : IKelasDal
    {
        private readonly DatabaseOptions _opt;

        public KelasDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Delete(IKelasKey key)
        {
            throw new NotImplementedException();
        }

        public KelasModel GetData(IKelasKey key)
        {
            const string sql = @"
            SELECT aa.fs_kd_kelas, aa.fs_nm_kelas, aa.fb_aktif,
            bb.fs_kd_kelas_dk
            FROM ta_kelas aa
            LEFT JOIN ta_kelas_dk bb ON aa.fs_kd_kelas_dk = bb.fs_kd_kelas_dk
            WHERE aa.fs_kd_kelas = @fs_kd_kelas
            ";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_kelas", key.KelasId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            KelasDto result = conn.ReadSingle<KelasDto>(sql, dp);
            return result?.ToModel()!;
        }

        public void Insert(KelasModel model)
        {
            const string sql = @"
            INSERT INTO ta_kelas (fs_kd_kelas, fs_nm_kelas, fb_aktif) 
            VALUES (@fs_kd_kelas, @fs_nm_kelas, '1')";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_kelas", model.KelasName, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public IEnumerable<KelasModel> ListData()
        {
            const string sql = @"
            SELECT fs_kd_kelas, fs_nm_kelas, fb_aktif, fs_kd_kelas_dk
            FROM ta_kelas
            ";
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<KelasDto>(sql);
            return result?.Select(x => x.ToModel());
        }

        public void Update(KelasModel model)
        {
            const string sql = @"
            UPDATE ta_kelas
            SET 
            fs_nm_kelas = @KelasName,
            fs_kd_kelas_dk = @KelasDkId,
            fb_aktif = @IsAktif
            WHERE fs_kd_kelas = @KelasId";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, new { model.KelasId, model.KelasName, model.KelasDkId,model.IsAktif });
        }

        public void Update(KelasModel model, KelasDkModel key)
        {
            const string sql = @"
            UPDATE ta_kelas
            SET fs_kd_kelas_dk = '@fs_kd_kelas_dk'
            WHERE fs_kd_kelas = '@fs_kd_kelas'
            ";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
            dp.AddParam("@fs_kd_kelas_dk", model.KelasDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }


        public class KelasDto
        {
            public string fs_kd_kelas { get; set; }
            public string fs_nm_kelas { get; set; }
            public bool fb_aktif { get; set; }
            public string fs_kd_kelas_dk { get; set; }


            public KelasModel ToModel()
            {
                var kelas = KelasModel.Create(fs_kd_kelas, fs_nm_kelas, fs_kd_kelas_dk);

                if (fb_aktif)
                    kelas.SetAktif();
                else
                    kelas.UnSetAktif();

                return kelas;
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
            var instalasi = KelasModel.Create("000", "NON KELAS", "C");
            _sut.Insert(instalasi);
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var instalasi = KelasModel.Create("101", "KELAS FLAT EARTH", "3");
            _sut.Update(instalasi);
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = KelasModel.Create("101", "KELAS FLAT EARTH",null);
            _sut.Insert(expected);
            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = KelasModel.Create("101", "KELAS FLAT EARTH", "3");
            _sut.Insert(expected);
            var actual = _sut.ListData();
            _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
        }

    }
}
