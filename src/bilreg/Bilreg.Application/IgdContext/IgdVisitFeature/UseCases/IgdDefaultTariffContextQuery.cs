using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdDefaultTariffContextQuery() : IRequest<IgdDefaultTariffContextResponse>;

public record IgdDefaultTariffContextResponse(
    string LayananId,
    string KelasId,
    string TipeJaminanId,
    string TipeTarifId);

public class IgdDefaultTariffContextHandler : IRequestHandler<IgdDefaultTariffContextQuery, IgdDefaultTariffContextResponse>
{
    private const string DEFAULT_JAMINAN_ID = "000";
    private const string DEFAULT_TIPE_JAMINAN_ID = "00000";

    private readonly IgdVisitOptions _options;
    private readonly IGetKelasRadarService _getKelasRadarService;
    private readonly IJaminanRepo _jaminanRepo;

    public IgdDefaultTariffContextHandler(
        IOptions<IgdVisitOptions> options,
        IGetKelasRadarService getKelasRadarService,
        IJaminanRepo jaminanRepo)
    {
        _options = options.Value;
        _getKelasRadarService = getKelasRadarService;
        _jaminanRepo = jaminanRepo;
    }

    public Task<IgdDefaultTariffContextResponse> Handle(
        IgdDefaultTariffContextQuery request,
        CancellationToken cancellationToken)
    {
        var layananId = _options.SmassLayananId;
        var kelas = _getKelasRadarService.Execute();
        var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(DEFAULT_JAMINAN_ID))
            .GetValueOrThrow($"Jaminan default '{DEFAULT_JAMINAN_ID}' tidak ditemukan");

        var response = new IgdDefaultTariffContextResponse(
            LayananId: layananId,
            KelasId: kelas.KelasId,
            TipeJaminanId: DEFAULT_TIPE_JAMINAN_ID,
            TipeTarifId: jaminan.TipeTarif.Rajal.TipeTarifId);

        return Task.FromResult(response);
    }
}
