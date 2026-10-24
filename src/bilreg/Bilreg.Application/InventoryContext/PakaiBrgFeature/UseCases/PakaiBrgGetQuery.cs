using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;

public record PakaiBrgGetQuery(string PakaiBrgId) : IRequest<PakaiBrgGetResponse>, IPakaiBrgKey;

public record PakaiBrgGetResponse(
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
    IReadOnlyList<PakaiBrgGetItemResponse> ListItem
);

public record PakaiBrgGetItemResponse(
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

public class PakaiBrgGetHandler : IRequestHandler<PakaiBrgGetQuery, PakaiBrgGetResponse>
{
    private readonly IPakaiBrgRepo _pakaiBrgRepo;

    public PakaiBrgGetHandler(IPakaiBrgRepo pakaiBrgRepo)
    {
        _pakaiBrgRepo = pakaiBrgRepo;
    }

    public Task<PakaiBrgGetResponse> Handle(PakaiBrgGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.PakaiBrgId, nameof(request.PakaiBrgId));

        var pakaiBrg = _pakaiBrgRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen pemakaian '{request.PakaiBrgId}' tidak ditemukan.")
            );

        var response = BuildResponse(pakaiBrg);
        return Task.FromResult(response);
    }

    private static PakaiBrgGetResponse BuildResponse(PakaiBrgModel model)
    {
        var items = model.ListItem.Select(x => new PakaiBrgGetItemResponse(
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

        return new PakaiBrgGetResponse(
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
