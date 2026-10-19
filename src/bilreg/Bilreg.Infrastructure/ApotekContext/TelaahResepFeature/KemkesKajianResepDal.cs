using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;

public record KemkesKajianDto
{
    public string KajianResepId { get; init; } = string.Empty;
    public string KajianResepName { get; init; } = string.Empty;
    public string GrupKajianResepId { get; init; } = string.Empty;
    public string GrupKajianResepName { get; init; } = string.Empty;
    public bool DefaultIsYes { get; init; }

    public KemkesKajianDto() { }

    public KemkesKajianDto(
        string kajianResepId,
        string kajianResepName,
        string grupKajianResepId,
        string grupKajianResepName,
        bool defaultIsYes)
    {
        KajianResepId = kajianResepId;
        KajianResepName = kajianResepName;
        GrupKajianResepId = grupKajianResepId;
        GrupKajianResepName = grupKajianResepName;
        DefaultIsYes = defaultIsYes;
    }
}

public interface IKemkesKajianResepDal : IListData<KemkesKajianDto>
{
}

public class KemkesKajianResepDal : IKemkesKajianResepDal
{
    private readonly DatabaseOptions _opt;

    public KemkesKajianResepDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IEnumerable<KemkesKajianDto> ListData()
    {
        const string sql = """
            SELECT 
                k.KajianResepId, 
                k.KajianResepName, 
                k.GrupKajianResepId, 
                g.GrupKajianResepName, 
                k.DefaultIsYes 
            FROM STD_KemkesKajianResep k 
            JOIN STD_KemkesGrupKajianResep g ON k.GrupKajianResepId = g.GrupKajianResepId 
            ORDER BY k.KajianResepId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<KemkesKajianDto>(sql);
    }
}
