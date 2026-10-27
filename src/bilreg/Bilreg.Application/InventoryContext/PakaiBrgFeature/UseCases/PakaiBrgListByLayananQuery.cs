using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;

public record PakaiBrgListByLayananQuery(string LayananId, string TglYmd1, string TglYmd2)
    : IRequest<IEnumerable<PakaiBrgListByLayananResponse>>;

public record PakaiBrgListByLayananResponse(
    string PakaiBrgId,
    string TglPakai,
    string JamPakai,
    string LayananId,
    string LayananName,
    string PetugasId,
    string Keterangan,
    bool IsVoid,
    string TglVoid,
    string PetugasVoidId,
    string AlasanVoid,
    decimal TotalNilai,
    IReadOnlyList<PakaiBrgListByLayananItemResponse> ListItem
);

public record PakaiBrgListByLayananItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    decimal Qty,
    string NoBatch,
    string TglEd,
    decimal Hpp,
    decimal SubTotal
);

public class PakaiBrgListByLayananHandler
    : IRequestHandler<PakaiBrgListByLayananQuery, IEnumerable<PakaiBrgListByLayananResponse>>
{
    private readonly IPakaiBrgRepo _pakaiBrgRepo;

    public PakaiBrgListByLayananHandler(IPakaiBrgRepo pakaiBrgRepo)
    {
        _pakaiBrgRepo = pakaiBrgRepo;
    }

    public Task<IEnumerable<PakaiBrgListByLayananResponse>> Handle(
        PakaiBrgListByLayananQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd");
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);

        var list = _pakaiBrgRepo.ListByLayanan(new LayananReff(request.LayananId, string.Empty), periode) ?? [];

        var sorted = list
            .OrderByDescending(x => x.TglPakai)
            .ThenByDescending(x => x.PakaiBrgId);

        var response = sorted.Select(BuildResponse);
        return Task.FromResult(response);
    }

    private static PakaiBrgListByLayananResponse BuildResponse(PakaiBrgModel model)
    {
        var items = model.ListItem.Select(x => new PakaiBrgListByLayananItemResponse(
            x.NoUrut,
            x.Brg.BrgId,
            x.Brg.BrgName,
            x.Satuan.SatuanId,
            x.Qty,
            x.NoBatch,
            x.TglEd == default ? string.Empty : x.TglEd.ToString("yyyy-MM-dd"),
            x.Hpp,
            x.SubTotal
        )).ToList();

        var tglVoid = model.IsVoid && model.TglVoid != default
            ? model.TglVoid.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;

        return new PakaiBrgListByLayananResponse(
            model.PakaiBrgId,
            model.TglPakai.ToString("yyyy-MM-dd"),
            model.TglPakai.ToString("HH:mm:ss"),
            model.Layanan.LayananId,
            model.Layanan.LayananName,
            model.PetugasId,
            model.Keterangan,
            model.IsVoid,
            tglVoid,
            model.PetugasVoidId,
            model.AlasanVoid,
            model.TotalNilai,
            items
        );
    }
}
