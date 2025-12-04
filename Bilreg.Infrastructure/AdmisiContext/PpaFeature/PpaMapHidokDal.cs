using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;


public interface IPpaMapHidokDal :
    IInsert<PpaMapHidokDto>,
    IUpdate<PpaMapHidokDto>,
    IDelete<IPpaMapHidokKey>,
    IGetData<PpaMapHidokDto, IPpaMapHidokKey>,
    IListData<PpaMapHidokDto>
{
}

public class PpaMapHidokDal : IPpaMapHidokDal
{
    private readonly DatabaseOptions _opt;

    public PpaMapHidokDal(IOptions<DatabaseOptions> opt) 
    {
        _opt = opt.Value;
    }

    public void Insert(PpaMapHidokDto dto)
    {
        const string sql = """
            INSERT INTO HiDok_MapDokter ( 
                DokterRs,
                DokterHidok
            )
            VALUES (
                @DokterRs,
                @DokterHidok
            )
            """;

        var dp = new DynamicParameters();
        dp.Add("@DokterRs", dto.DokterRs, DbType.String);
        dp.Add("@DokterHidok", dto.DokterHidok, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt)); // Gunakan helper Anda
        conn.Execute(sql, dp);
    }

    public void Update(PpaMapHidokDto dto)
    {
        const string sql = """
            UPDATE HiDok_MapDokter 
            SET
                DokterRs = @DokterRs
            WHERE
                DokterHidok = @DokterHidok
            """;

        var dp = new DynamicParameters();
        dp.Add("@DokterRs", dto.DokterRs, DbType.String);
        dp.Add("@DokterHidok", dto.DokterHidok, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt)); 
        conn.Execute(sql, dp);
    }

    public void Delete(IPpaMapHidokKey key)
    {
        const string sql = """
            DELETE FROM HiDok_MapDokter
            WHERE
                DokterHidok = @DokterHidok
            """;

        var dp = new DynamicParameters();
        dp.Add("@DokterHidok", key.PpaHidokId, DbType.String); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt)); 
        conn.Execute(sql, dp);
    }

    public PpaMapHidokDto GetData(IPpaMapHidokKey key)
    {
        const string sql = """
            SELECT
                aa.DokterRs,
                ISNULL(bb.fs_nm_peg,'') AS DokterName,
                aa.DokterHidok
            FROM HiDok_MapDokter aa
            LEFT JOIN td_peg bb ON aa.DokterRS = bb.FS_KD_PEG 
            WHERE
                aa.DokterHidok = @DokterHidok
            """;

        var dp = new DynamicParameters();
        dp.Add("@DokterHidok", key.PpaHidokId, DbType.String); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PpaMapHidokDto>(sql, dp);
    }

    public IEnumerable<PpaMapHidokDto> ListData()
    {
        const string sql = """
            SELECT
                aa.DokterRs,
                ISNULL(bb.fs_nm_peg,'') AS DokterName,
                aa.DokterHidok
            FROM HiDok_MapDokter aa
                LEFT JOIN td_peg bb ON aa.DokterRS = bb.FS_KD_PEG  
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt)); 
        return conn.Query<PpaMapHidokDto>(sql);
    }
}