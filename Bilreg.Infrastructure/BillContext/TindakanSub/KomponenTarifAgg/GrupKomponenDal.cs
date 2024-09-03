using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.KomponenTarifAgg
{
    public class GrupKomponenDal : IGrupKomponenDal
    {
        private readonly DatabaseOptions _opt;

        public GrupKomponenDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Delete(IGrupKomponenKey key)
        {
            throw new NotImplementedException();
        }

        public GrupKomponenModel GetData(IGrupKomponenKey key)
        {
            const string sql = @"
                SELECT 
                    fs_kd_grup_detil_tarif,
                    fs_nm_grup_detil_tarif
                )
                FROM ta_grup_detil_tarif
                WHERE
                    fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif;
                )";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_grup_detil_tarif", key.GrupKomponenId, SqlDbType.VarChar);
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<GrupKomponenDto>(sql, dp);
            return result?.ToModel()!;
        }

        public void Insert(GrupKomponenModel model)
        {
            const string sql = @"
                INSERT INTO ta_grup_detil_tarif(
                    fs_kd_grup_detil_tarif,
                    fs_nm_grup_detil_tarif
                )
                VALUES(
                    @fs_kd_grup_detil_tarif,
                    @fs_nm_grup_detil_tarif
                )";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_grup_detil_tarif", model.GrupKomponenId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_grup_detil_tarif", model.GrupKomponenName, SqlDbType.VarChar);
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public IEnumerable<GrupKomponenModel> ListData()
        {
            throw new NotImplementedException();
        }

        public void Update(GrupKomponenModel model)
        {
            throw new NotImplementedException();
        }
    }

    public class GrupKomponenDto(
        string fs_kd_grup_detil_tarif,
        string fs_nm_grup_detil_tarif
        )
    {
        public GrupKomponenModel ToModel()
        {
            var gk = new GrupKomponenModel(fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif);
            return gk;
        }
    }
}
