
using Bilreg.Domain.AdmisiContext.RemotCetakFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.RemoteCetakFeature;

public interface IRemoteCetakDal :
    IInsert<RemoteCetakDto>
{ }
public class RemoteCetakDal : IRemoteCetakDal
{
    private readonly DatabaseOptions _opt;

    public RemoteCetakDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RemoteCetakDto dto)
    {
        const string sql = """
           INSERT INTO ta_remote_cetak(
                fs_kd_trs, fs_jenis_dok,  
                fd_tgl_send, fs_jam_send, fs_remote_addr, 
                fn_cetak, fd_tgl_cetak, 
                fs_jam_cetak, fs_json_data, 
                CallbackDataOfta)
           VALUES( 
                @fs_kd_trs, @fs_jenis_dok, 
                @fd_tgl_send, @fs_jam_send, @fs_remote_addr, 
                @fn_cetak, @fd_tgl_cetak, 
                @fs_jam_cetak, @fs_json_data, 
                @CallbackDataOfta)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_jenis_dok", dto.fs_jenis_dok, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_send", dto.fd_tgl_send, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_send", dto.fs_jam_send, SqlDbType.VarChar);
        dp.AddParam("@fs_remote_addr", dto.fs_remote_addr, SqlDbType.VarChar);
        dp.AddParam("@fn_cetak", dto.fn_cetak, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_cetak", dto.fd_tgl_cetak, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_cetak", dto.fs_jam_cetak, SqlDbType.VarChar);
        dp.AddParam("@fs_json_data", dto.fs_json_data, SqlDbType.VarChar);
        dp.AddParam("@CallbackDataOfta", dto.CallbackDataOfta, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}

public record RemoteCetakDto(string fs_kd_trs, string fs_jenis_dok, string fd_tgl_send,
    string fs_jam_send, string fs_remote_addr, decimal fn_cetak, string fd_tgl_cetak,
    string fs_jam_cetak, string fs_json_data, string CallbackDataOfta)
{
    public static RemoteCetakDto FromModel(RemoteCetakType model)
    {
        var fn_cetak = model.IsCetak ? 1 : 0;

        var result = new RemoteCetakDto(
            model.TransaksiId,
            model.JenisDokumen,
            model.TanggalSend.ToString("yyyy-MM-dd"),
            model.TanggalSend.ToString("HH:mm:ss"),
            model.RemoteAddress,
            fn_cetak,
            model.TanggalCetak.ToString("yyyy-MM-dd"),
            model.TanggalCetak.ToString("HH:mm:ss"),
            model.JsonData,
            model.CallbackData
            );
        return result;
    }

    public static RemoteCetakType ToModel(RemoteCetakDto dto)
    {
        var tglSend = $"{dto.fd_tgl_send} {dto.fs_jam_send}".ToDate("yyyy-MM-dd HH:mm:ss");
        var tglCetak = $"{dto.fd_tgl_cetak} {dto.fs_jam_cetak}".ToDate("yyyy-MM-dd HH:mm:ss");
        var isCetak = dto.fn_cetak == 1 ? true : false;
        var result = new RemoteCetakType(dto.fs_kd_trs, dto.fs_jenis_dok, tglSend,
            dto.fs_remote_addr, isCetak, tglCetak, dto.fs_json_data, dto.CallbackDataOfta);
        return result;
    }
}

