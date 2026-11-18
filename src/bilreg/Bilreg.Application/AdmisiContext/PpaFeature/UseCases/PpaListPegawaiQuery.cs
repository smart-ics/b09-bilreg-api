using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListPegawaiQuery(string? Keyword = null)
    : IRequest<IEnumerable<PpaListPegawaiResponse>>;

public record PpaListPegawaiResponse(
    string PpaId,
    string PpaName);

public class PpaListPegawaiHandler
    : IRequestHandler<PpaListPegawaiQuery, IEnumerable<PpaListPegawaiResponse>>
{
    private readonly IPpaRepo _ppaRepo;

    public PpaListPegawaiHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListPegawaiResponse>> Handle(
        PpaListPegawaiQuery request,
        CancellationToken cancellationToken)
    {
        var listPegawai = _ppaRepo.ListData()?.ToList() ?? [];

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            listPegawai = listPegawai
                .Where(x =>
                    x.PpaName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    x.PpaId.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var result = listPegawai
            .Select(x => new PpaListPegawaiResponse(x.PpaId, x.PpaName));

        return Task.FromResult(result);
    }
}
