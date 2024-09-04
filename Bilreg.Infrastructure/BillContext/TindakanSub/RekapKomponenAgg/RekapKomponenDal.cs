using Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.RekapKomponenAgg
{
    public class RekapKomponenDal:IRekapKomponenDal
    {
        private readonly DatabaseOptions _opt;
        public RekapKomponenDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Delete(IRekapKomponenKey key)
        {
            const string sql = @"
                DELETE FROM
                    ta_rekap_komponen
                WHERE
                    fs_kd_rekap_komponen = @fs_kd_rekap_komponen;
                ";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_rekap_komponen", key.RekapKomponenId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public RekapKomponenModel GetData(IRekapKomponenKey key)
        {
            const string sql = @"
                SELECT
                    fs_kd_rekap_komponen,
                    fs_nm_rekap_komponen,
                    fn_urut
                FROM 
                    ta_rekap_komponen
                WHERE
                    fs_kd_rekap_komponen = @fs_kd_rekap_komponen;
            ";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_rekap_komponen", key.RekapKomponenId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.ReadSingle<RekapKomponenDto>(sql,dp);
        }

        public void Insert(RekapKomponenModel model)
        {
            const string sql = @"
            INSERT INTO ta_rekap_komponen(
                fs_kd_rekap_komponen,
                fs_nm_rekap_komponen,
                fn_urut
            )
            VALUES(
                @fs_kd_rekap_komponen,
                @fs_nm_rekap_komponen,
                @fn_urut
            );";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_rekap_komponen", model.RekapKomponenId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_rekap_komponen", model.RekapKomponenName, SqlDbType.VarChar);
            dp.AddParam("@fn_urut", model.RekapKomponenUrut, SqlDbType.Decimal);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public IEnumerable<RekapKomponenModel> ListData()
        {
            const string sql = @"
                SELECT
                    fs_kd_rekap_komponen,
                    fs_nm_rekap_komponen,
                    fn_urut
                FROM
                    ta_rekap_komponen
                WHERE
                    fs_kd_rekap_komponen = fs_kd_rekap_komponen;
            ";
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.Read<RekapKomponenDto>(sql);
        }

        public void Update(RekapKomponenModel model)
        {
            const string sql = @"
                UPDATE
                    ta_rekap_komponen
                SET
                    fs_kd_rekap_komponen= @fs_kd_rekap_komponen,
                    fs_nm_rekap_komponen= @fs_nm_rekap_komponen,
                    fn_urut= @fn_urut
                WHERE
                    fs_kd_rekap_komponen= @fs_kd_rekap_komponen
                ";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_rekap_komponen", model.RekapKomponenId,SqlDbType.VarChar);
            dp.AddParam("@fs_nm_rekap_komponen", model.RekapKomponenId,SqlDbType.VarChar);
            dp.AddParam("fn_urut", model.RekapKomponenId,SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }
    }

    public class RekapKomponenDto:RekapKomponenModel
    {
        public RekapKomponenDto(): base(string.Empty, string.Empty, decimal.Zero)
        {

        }
        public string fs_kd_rekap_komponen { get => RekapKomponenId; set => RekapKomponenId = value; }
        public string fs_nm_rekap_komponen { get => RekapKomponenName; set => RekapKomponenName = value; }
        public decimal fn_urut { get => RekapKomponenUrut; set => RekapKomponenUrut = value; }
    }
}
