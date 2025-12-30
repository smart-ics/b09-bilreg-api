using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record NilaiTarifListQuery(string LayananId, string TipeTarifId, string KelasId) 
    : IRequest<IEnumerable<NilaiTarifListResponse>>, ILayananKey, INilaiTarifVariant;

public record NilaiTarifListResponse(string TarifId, string TarifName,
    TipeTarifReff TipeTarif, KelasReff Kelas,
    decimal Nilai);

public class NilaiTarifListHandler : IRequestHandler<NilaiTarifListQuery, IEnumerable<NilaiTarifListResponse>>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;

    public NilaiTarifListHandler(INilaiTarifRepo nilaiTarifRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task<IEnumerable<NilaiTarifListResponse>> Handle(NilaiTarifListQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TipeTarifId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);

        var listNilai = _nilaiTarifRepo.ListData(request, request);
        var resut = listNilai.Select(x => new NilaiTarifListResponse(
            x.TarifId, x.TarifName, new TipeTarifReff(x.TipeTarif.TipeTarifId, x.TipeTarif.TipeTarifName),
            new KelasReff(x.Kelas.KelasId, x.Kelas.KelasName), x.Nilai));
        return Task.FromResult(resut);
    }
}
