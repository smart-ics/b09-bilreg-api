using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

public class PolisDal : IPolisDal
{
    private readonly DatabaseOptions _opt;

    public PolisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PolisModel model)
    {
        const string sql = @"
            INSERT INTO ta_polis(
                fs_kd_polis, fs_no_polis, fs_atas_nama, fd_txpired, 
                fs_kd_tipe_jaminan, fb_cover_rj)
            VALUES(
                @fs_kd_polis, @fs_no_polis, @fs_atas_nama, @fd_txpired, 
                @fs_kd_tipe_jaminan, @fb_cover_rj
            )";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", model.PolisId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", model.NoPolis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", model.AtasNama, SqlDbType.VarChar);
        dp.AddParam("@fd_txpired", model.ExpiredDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", model.IsCoverRajal, SqlDbType.Bit);
    }

    public void Update(PolisModel model)
    {
        const string sql = @"
            UPDATE ta_polis
            SET 
                fs_no_polis = @fs_no_polis,
                fs_atas_nama = @fs_atas_nama,
                fd_txpired = @fd_txpired,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fb_cover_rj = @fb_cover_rj
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", model.PolisId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", model.NoPolis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", model.AtasNama, SqlDbType.VarChar);
        dp.AddParam("@fd_txpired", model.ExpiredDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", model.IsCoverRajal, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPolisKey key)
    {
        const string sql = @"
            DELETE FROM ta_polis
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PolisModel GetData(IPolisKey key)
    {
        const string sql = @"
            SELECT
                fs_kd_polis, fs_no_polis, fs_atas_nama,
                fd_txpired, fs_kd_tipe_jaminan, fb_cover_rj
            FROM ta_polis
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PolisDto>(sql, dp);
    }

    public IEnumerable<PolisModel> ListData(IPasienKey filter)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<PolisModel> ListData(string filter)
    {
        throw new NotImplementedException();
    }
}

internal class PolisDto : PolisModel
{
    public PolisDto() : base(string.Empty)
    {
    }
    public string fs_kd_polis {get => PolisId; set => PolisId = value;}
    public string fs_kd_tipe_jaminan {get => TipeJaminanId; set => TipeJaminanId = value;}
    public string fs_nm_tipe_jaminan {get => TipeJaminanName; set => TipeJaminanName = value}
    public string fs_no_polis {get => NoPolis; set => NoPolis = value;}
    public string fs_atas_nama {get => AtasNama; set => AtasNama = value;}
    public string fd_expired {get => ExpiredDate.ToString("yyyy-MM-dd"); set => ExpiredDate = value.ToDate(DateFormatEnum.YMD);}
    public string fs_kd_kelas_ri {get => KelasId; set => KelasId = value;}
    public string fs_nm_kelas_ri {get => KelasName; set => KelasName = value;}
    public bool fb_cover_rj {get => IsCoverRajal; set => IsCoverRajal = value;}
}