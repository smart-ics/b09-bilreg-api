using Ardalis.GuardClauses;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record DeepSearchPasienQuery(string Keyword, string SearchMode) : IRequest<IEnumerable<DeepSearchPasienResponse>>;

public record DeepSearchPasienResponse(
    string PasienId, string PasienName, string TglLahir,
    string Gender, string NoIdentitas, string JenisIdentitas,
    string IbuKandung, string Alamat, string RegId, string BookingId);

public class DeepSearchPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<DeepSearchPasienResponse>>
{
    private readonly IParamSistemDal _paramSistemDal;
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public DeepSearchPasienHandler(IParamSistemDal paramSistemDal)
    {
        _paramSistemDal = paramSistemDal;
    }

    public Task<IEnumerable<DeepSearchPasienResponse>> Handle(DeepSearchPasienQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");
        var kodeRs = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var finder = PasienFinder.CreateNew(request.Keyword, kodeRs);
        var datasource = new List<PasienFinder>();
        // if (request.SearchMode == "QUICK")
        //     datasource = PopulateQuickSearch(request.Keyword);
        // else
        //     datasource = finder.TglLahir != string.Empty
        //         ? PopulateDeepSearchL1(request.Keyword)
        //         : PopulateDeepSearchL2(request.Keyword);

        throw new NotImplementedException();
    }

    private List<PasienFinder> PopulateQuickSearch(string requestKeyword)
    {
        throw new NotImplementedException();
    }
}        
