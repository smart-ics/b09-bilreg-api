﻿using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
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

namespace Bilreg.Infrastructure.AdmPasienContext.StatusKawinAgg
{
    public class StatusKawinDal : IStatusKawinDal
    {
        private readonly DatabaseOptions _opt;

        public StatusKawinDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }
        public void Insert(StatusKawinModel model)
        {
            //  Query
            const string sql = @"
                INSERT INTO ta_status_kawin_dk(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk)
                VALUES (@fs_kd_status_kawin_dk, @fs_nm_status_kawin_dk)";

            //  Param
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinName, SqlDbType.VarChar);

            //  Execute
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Update(StatusKawinModel model)
        {
            //  QUERY
            const string sql = @"
                UPDATE ta_status_kawin_dk
                SET fs_nm_status_kawin_dk = @fs_nm_status_kawin_dk;
                WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinName, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);

        }

        public void Delete(IStatusKawinKey key)
        {
            //Query
            const string sql = @"DELETE FROM ta_status_kawin_dk 
                                 WHERE @fs_kd_status_kawin_dk";
            //Param
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinId, SqlDbType.VarChar);

            //Execute
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public StatusKawinModel GetData(IStatusKawinKey key)
        {
            const string sql = @"
            SELECT  fs_kd_status_kawin_dk, fs_nm_status_kawin_dk
            FROM ta_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<StatusKawinDto>(sql, dp);
            return result?.ToModel();
        }

        public IEnumerable<StatusKawinModel> ListData()
        {
            const string sql = @"
            SELECT  fs_kd_status_kawin_dk, fs_nm_status_kawin_dk
            FROM ta_status_kawin_dk ";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<StatusKawinDto>(sql);
            return result?.Select(x => x.ToModel());
        }

        public class StatusKawinDto
        {
            public string fs_kd_status_kawin_dk { get; set; }
            public string fs_nm_kd_status_kawin_dk { get; set; }
            public StatusKawinModel ToModel() => StatusKawinModel.Create(fs_kd_status_kawin_dk, fs_nm_kd_status_kawin_dk);
        }


    }

    public class StatusKawinDalTest
    {
        private readonly StatusKawinDal _skut;

        public StatusKawinDalTest()
        {
            _skut = new StatusKawinDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            _skut.Insert(StatusKawinModel.Create("A", "B"));
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            _skut.Update(StatusKawinModel.Create("A", "B"));
        }
        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            _skut.Delete(StatusKawinModel.Create("A", "B"));
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = StatusKawinModel.Create("A", "B");
            _skut.Insert(expected);
            var actual = _skut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GivenNotExistDate_ThenReturnNull()
        {
            using var trans = TransHelper.NewScope();
            var expected = StatusKawinModel.Create("A", "B");
            var actual = _skut.GetData(expected);
            actual.Should().BeNull();
        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new List<StatusKawinModel> { StatusKawinModel.Create("A", "B") };
            _skut.Insert(StatusKawinModel.Create("A", "B"));
            var actual = _skut.ListData();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
