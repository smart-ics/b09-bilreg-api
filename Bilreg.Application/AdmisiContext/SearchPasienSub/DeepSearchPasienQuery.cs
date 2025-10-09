using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record DeepSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienModel>>;

public class DeepSeachPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<SearchPasienModel>>
{
    private readonly IDeepSearchPasienDal _deepSearchDal;

    public DeepSeachPasienHandler(IDeepSearchPasienDal deepSearchDal)
    {
        _deepSearchDal = deepSearchDal;
    }

    public Task<IEnumerable<SearchPasienModel>> Handle(DeepSearchPasienQuery request, CancellationToken cancellationToken)
    {
        var listData = _deepSearchDal.ListData()
            .Match(
                some => some,
                () => throw new InvalidOperationException("data not found")
            );
        var result = new List<SearchPasienModel>();
        var pasienByName = SearchByPasienName(listData, request.Keyword);
        var pasienByTglLahir = SearchByTglLahir(listData, request.Keyword);
        var pasienByPasienId = SearchByPasienId(listData, request.Keyword);
        var pasienByRegId = SearchByRegId(listData, request.Keyword); 

        result.AddRange(pasienByName);
        result.AddRange(pasienByTglLahir);
        result.AddRange(pasienByPasienId);
        result.AddRange(pasienByRegId);

        return Task.FromResult(result.AsEnumerable());
    }

    private IEnumerable<SearchPasienModel> SearchByPasienName(IEnumerable<SearchPasienModel> listData , string name)
    {
        var result = listData
            .Where(x => x.PasienName.ToLower().Contains(name.ToLower()))
            .ToList() ?? new List<SearchPasienModel>();
        return result;
    }

    private IEnumerable<SearchPasienModel> SearchByTglLahir(IEnumerable<SearchPasienModel> listData, string tgllahir)
    {
        var result = listData
            .Where(x => x.TglLahir.ToString("yyyy-MM-dd") == tgllahir)
            .ToList() ?? new List<SearchPasienModel>();
        return result;
    }

    private IEnumerable<SearchPasienModel> SearchByPasienId(IEnumerable<SearchPasienModel> listData, string pasienId)
    {
        var result = listData
            .Where(x => x.PasienId == pasienId)
            .ToList() ?? new List<SearchPasienModel>();
        return result;
    }

    private IEnumerable<SearchPasienModel> SearchByRegId(IEnumerable<SearchPasienModel> listData, string regId)
    {
        var result = listData
            .Where(x => x.RegId == regId)
            .ToList() ?? new List<SearchPasienModel>();
        return result;
    }
}
