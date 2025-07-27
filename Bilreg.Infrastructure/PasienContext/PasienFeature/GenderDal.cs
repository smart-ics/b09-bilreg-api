using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class GenderDal : IGenderDal
{
    private readonly DatabaseOptions _opt;
    private readonly Dictionary<string, GenderType> _mapDictionary;

    public GenderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
        
        const string sql = @"
            SELECT fs_kd_jenis_kelamin, fs_nm_jenis_kelamin, fs_sex_dk
            FROM ta_jenis_kelamin ";

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dtos = conn.Read<GenderDto>(sql);
        var result = dtos.Select(x => x.ToModel()).ToList();
        _mapDictionary = result.ToDictionary(x => x.Symbol, x => x);
    }

    public MayBe<GenderType> GetData(string symbol)
    {
        var result = MayBe.From(_mapDictionary.GetValueOrDefault(symbol));
        return result!;
    }
}

public record GenderDto(
    string fs_kd_jenis_kelamin,
    string fs_nm_jenis_kelamin,
    string fs_sex_dk)
{
    public GenderType ToModel() => new(
        fs_kd_jenis_kelamin,
        fs_nm_jenis_kelamin,
        fs_sex_dk == "1" ? SexDkEnum.Male : SexDkEnum.Female);
}