using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;

public record BhpRanapAddCmd(
    string PakaiBedId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string UserId,
    string RegId = "",
    string LayananId = "")
    : IRequest<BhpRanapAddResponse>, IPakaiBed;

public record BhpRanapAddResponse(
    string BhpRanapId,
    string PakaiBedId,
    string RegId,
    decimal Subtotal);

public class BhpRanapAddHandler : IRequestHandler<BhpRanapAddCmd, BhpRanapAddResponse>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBhpRanapRepo _bhpRanapRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public BhpRanapAddHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBhpRanapRepo bhpRanapRepo,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bhpRanapRepo = bhpRanapRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BhpRanapAddResponse> Handle(BhpRanapAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.BhpItemId, nameof(request.BhpItemId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));
        Guard.Against.Negative(request.Price, nameof(request.Price));

        string resolvedRegId = request.RegId;
        string resolvedLayananId = request.LayananId;

        // Try load occupancy details from PakaiBedRepo if not provided
        var maybeBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId));
        if (maybeBed.HasValue)
        {
            var bed = maybeBed.Value;
            if (string.IsNullOrWhiteSpace(resolvedRegId))
                resolvedRegId = bed.Reg?.RegId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedLayananId))
                resolvedLayananId = bed.Layanan?.LayananId ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(resolvedRegId))
        {
            throw new InvalidOperationException($"RegId tidak ditemukan untuk PakaiBed '{request.PakaiBedId}'.");
        }

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var bhp = BhpRanapModel.Create(
            pakaiBedId: request.PakaiBedId,
            regId: resolvedRegId,
            layananId: resolvedLayananId,
            bhpItemId: request.BhpItemId,
            bhpItemName: request.BhpItemName,
            qty: request.Qty,
            price: request.Price,
            audit: audit);

        using (var trans = TransHelper.NewScope())
        {
            _bhpRanapRepo.SaveChanges(bhp);
            trans.Complete();
        }

        var response = new BhpRanapAddResponse(bhp.BhpRanapId, bhp.PakaiBedId, bhp.RegId, bhp.Subtotal);
        return Task.FromResult(response);
    }
}
