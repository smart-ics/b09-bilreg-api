using Bilreg.Application.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSearchQuery(string Keyword) :IRequest<IEnumerable<PasienPersonView>> ;

public class PasienSearchHandler : IRequestHandler<PasienSearchQuery, IEnumerable<PasienPersonView>>
{
    private readonly IPasienRepo _repo;
    private const int LIMIT_CONTER = 200;

    public PasienSearchHandler(IPasienRepo repo) 
        => _repo = repo;

    public Task<IEnumerable<PasienPersonView>> Handle(PasienSearchQuery request, CancellationToken cancellationToken)
    {
        var isNik = IsKeywordNik(request.Keyword);
        var isNoPesertaBpjs = IsNoPesertaBpjs(request.Keyword);
        var isNoRujukan = IsNoRUjukanBpjs(request.Keyword);
        var isNameTglLahir = !isNik && !isNoPesertaBpjs && !isNoRujukan;
        
        List<PasienPersonView> result = new();

        if (isNik)
        {
            var resultNik = GetPasienByNik(request.Keyword);
            result = result.Add(resultNik);
        }

        if (isNoPesertaBpjs)
        {
            
        }

        if (isNoRujukan)
        {
            
        }

        if (isNameTglLahir)
        {
            var resultNamaTglLahir = _repo.SearchPasien(request.Keyword)?.ToList() ?? [];
            result.AddRange(resultNameTglLahir);
        }

        if (result.Count > LIMIT_CONTER)
            throw new TooManyResultsException(LIMIT_CONTER, "Gunakan keyword search lebih spesifik");
        
        return Task.FromResult(result.AsEnumerable());
    }
}
