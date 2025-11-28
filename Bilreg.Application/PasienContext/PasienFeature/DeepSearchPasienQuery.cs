using Ardalis.GuardClauses;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record DeepSearchPasienQuery(string Keyword, string SearchMode) : IRequest<IEnumerable<DeepSearchPasienResponse>>;

public record DeepSearchPasienResponse(
    string PasienId, string PasienName, string TglLahir,
    string Gender, string NoIdentitas, string JenisIdentitas,
    string IbuKandung, string Alamat, string RegId, string BookingId);

public class DeepSearchPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<DeepSearchPasienResponse>>
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly IPasienRepo _pasienRepo;
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public DeepSearchPasienHandler(IParamSistemDal paramSistemDal, 
        IPasienRepo pasienRepo)
    {
        _paramSistemDal = paramSistemDal;
        _pasienRepo = pasienRepo;
    }

    public Task<IEnumerable<DeepSearchPasienResponse>> Handle(DeepSearchPasienQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 3)
            throw new ArgumentException("Keyword terlalu pendek (minimal 3 karakter).");
        var kodeRs = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var finder = PasienFinder.CreateNew(request.Keyword, kodeRs);

        var datasource = new List<PasienFinder>();
        if (request.SearchMode == "QUICK")
            datasource = DataSourceQuickSearch(request.Keyword);
        else
            datasource = DataSourceDeepSearch(request.Keyword);

        throw new NotImplementedException();
    }

    private List<PasienFinder> DataSourceQuickSearch(string requestKeyword)
    {
        throw new NotImplementedException();
    }
    private List<PasienFinder> DataSourceDeepSearch(string requestKeyword)
    {
        throw new NotImplementedException();
    }
}        
