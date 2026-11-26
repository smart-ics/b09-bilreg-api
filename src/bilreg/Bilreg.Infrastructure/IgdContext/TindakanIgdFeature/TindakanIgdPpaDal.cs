using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public interface ITindakanIgdPpaDal :
    IInsertBulk<TindakanIgdPpaDto>,
    IDelete<ITindakanIgdKey>,
    IListData<TindakanIgdPpaDto, ITindakanIgdKey>
{
}

public class TindakanIgdPpaDal : ITindakanIgdPpaDal
{
    private readonly DatabaseOptions _opt;

    public TindakanIgdPpaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TindakanIgdPpaDto> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_TindakanIgdPpa (
                TindakanIgdId, KomponenId, PpaId, PpaName)
            VALUES (
                @TindakanIgdId, @KomponenId, @PpaId, @PpaName)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var dto in list)
        {
            var dp = new DynamicParameters();
            dp.AddParam("@TindakanIgdId", dto.TindakanIgdId, SqlDbType.VarChar);
            dp.AddParam("@KomponenId", dto.KomponenId, SqlDbType.VarChar);
            dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
            dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
            conn.Execute(sql, dp);
        }
    }

    public void Delete(ITindakanIgdKey key)
    {
        const string sql = "DELETE BILRG_TindakanIgdPpa WHERE TindakanIgdId = @TindakanIgdId";
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", key.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TindakanIgdPpaDto> ListData(ITindakanIgdKey filter)
    {
        const string sql = """
            SELECT TindakanIgdId, KomponenId, PpaId, PpaName
            FROM BILRG_TindakanIgdPpa
            WHERE TindakanIgdId = @TindakanIgdId
            ORDER BY KomponenId, PpaId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", filter.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanIgdPpaDto>(sql, dp);
    }
}
