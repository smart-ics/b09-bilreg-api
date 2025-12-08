using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public interface IPpaContactDal :
    IListData<ContactType, IPpaKey>
{
}

public class PpaContactDal : IPpaContactDal
{
    private readonly DatabaseOptions _opt;

    public PpaContactDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<ContactType> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT 3 as JenisContact, fs_email ContactDetail FROM td_peg WHERE fs_kd_peg = @fs_kd_peg UNION
            SELECT 2 as JenisContact, fs_hp_peg ContactDetail FROM td_peg WHERE fs_kd_peg = @fs_kd_peg UNION
            SELECT 1 as JenisContact, fs_tlp_peg ContactDetail FROM td_peg WHERE fs_kd_peg = @fs_kd_peg UNION
            SELECT 3 AS JenisContact, DokterHidok ContactDetail FROM HIDOK_MapDokter WHERE DokterRs = @fs_kd_peg
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PpaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var listAll = conn.Query<ContactType>(sql, dp);
        var result = listAll
            .Select(x => x with { JenisContact = (JenisContactEnum)x.JenisContact })
            .Where(x => x.ContactDetail.Trim().Length > 0);
        return result;
    }
}