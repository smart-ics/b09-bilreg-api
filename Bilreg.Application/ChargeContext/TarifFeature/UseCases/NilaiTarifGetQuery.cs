using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record NilaiTarifGetQuery(string TarifId, string TipeTarifId, string KelasId) :
    IRequest<NilaiTarifGetResponse>, INilaiTarifCompositKey;

public record NilaiTarifGetResponse(string TarifId, string TarifName,
    TipeTarifReff TipeTarif, KelasReff Kelas,
    decimal Nilai); 
public class NilaiTarifGetHandler : IRequestHandler<NilaiTarifGetQuery, NilaiTarifGetResponse>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;

    public NilaiTarifGetHandler(INilaiTarifRepo nilaiTarifRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task<NilaiTarifGetResponse> Handle(NilaiTarifGetQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.TarifId);
        Guard.Against.NullOrWhiteSpace(request.TipeTarifId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);


        var nilaiTarif = _nilaiTarifRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tarif {request.TarifId} not found")
            );
        var result = new NilaiTarifGetResponse(nilaiTarif.TarifId, nilaiTarif.TarifName, 
            nilaiTarif.TipeTarif, nilaiTarif.Kelas, nilaiTarif.Nilai);
        return Task.FromResult(result);
    }
}
