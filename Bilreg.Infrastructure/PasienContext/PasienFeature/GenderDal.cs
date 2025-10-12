using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class GenderDal : IGenderDal
{
    private readonly PasienContextOptions _opt;

    public GenderDal(IOptions<PasienContextOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<GenderType> ListData()
    {
        var genderMale = new GenderType(_opt.GenderType.Male, "Laki-Laki", SexDkEnum.Male);
        var genderFemale = new GenderType(_opt.GenderType.Female, "Perempuan", SexDkEnum.Female);
        return new List<GenderType> { genderMale, genderFemale };
    }
    public MayBe<GenderType> GetData(string symbol)
    {
        var result = ListData()
            .FirstOrDefault(x => string.Equals(x.Symbol, symbol, StringComparison.CurrentCultureIgnoreCase));
        return MayBe.From(result!);
    }   
}
