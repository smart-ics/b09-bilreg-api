using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;

public record IgdTindakanPpaCmd(string KomponenId, string PpaId);

public record IgdTindakanAddCmd(
    string IgdVisitId,
    string PetugasMedisId,
    string ReffId,
    string Descriptions,
    int Qty,
    int Aktifitas,
    string UserId,
    string? LayananId = null,
    IEnumerable<IgdTindakanPpaCmd>? ListPpa = null)
    : IRequest<IgdTindakanAddResponse>, IIgdVisitKey;

public record IgdTindakanAddResponse(string TindakanIgdId, string IgdVisitId);

public class IgdTindakanAddHandler : IRequestHandler<IgdTindakanAddCmd, IgdTindakanAddResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITindakanIgdRepo _tindakanRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegRepo _regRepo;
    private readonly IIgdBillingPostingOrchestrator _billingOrchestrator;
    private readonly IIgdStockService _igdStockService;
    private readonly IgdVisitOptions _igdVisitOptions;

    public IgdTindakanAddHandler(
        IIgdVisitRepo igdVisitRepo,
        ITindakanIgdRepo tindakanRepo,
        IPpaRepo ppaRepo,
        ITglJamProvider tglJamProvider,
        IRegRepo regRepo,
        IIgdBillingPostingOrchestrator billingOrchestrator,
        IIgdStockService igdStockService,
        IOptions<IgdVisitOptions> igdVisitOptions)
    {
        _igdVisitRepo = igdVisitRepo;
        _tindakanRepo = tindakanRepo;
        _ppaRepo = ppaRepo;
        _tglJamProvider = tglJamProvider;
        _regRepo = regRepo;
        _billingOrchestrator = billingOrchestrator;
        _igdStockService = igdStockService;
        _igdVisitOptions = igdVisitOptions.Value;
    }

    public async Task<IgdTindakanAddResponse> Handle(IgdTindakanAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.Null(request.Aktifitas, nameof(request.Aktifitas));
        var aktifitas = (ActivityTindakanIgd)request.Aktifitas;

        var validListPpaInput = request.ListPpa?
            .Where(x => !string.IsNullOrWhiteSpace(x.PpaId))
            .ToList() ?? [];

        if (aktifitas == ActivityTindakanIgd.Tindakan
            && string.IsNullOrWhiteSpace(request.PetugasMedisId)
            && validListPpaInput.Count == 0)
        {
            Guard.Against.NullOrWhiteSpace(request.PetugasMedisId, nameof(request.PetugasMedisId));
        }

        Guard.Against.NullOrWhiteSpace(request.ReffId, nameof(request.ReffId));
        Guard.Against.NullOrWhiteSpace(request.Descriptions, nameof(request.Descriptions));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var reg = visit.HasReg
            ? _regRepo.LoadEntity(visit.Reg).GetValueOrThrow($"Reg '{visit.Reg.RegId}' not found")
            : null;
        var effectiveLayananId = ResolveEffectiveLayananId(request.LayananId, reg);

        var (primaryPpa, listPpa) = ResolvePpa(request.PetugasMedisId, validListPpaInput);

        var occurredAt = _tglJamProvider.Now;
        var tindakan = TindakanIgdModel.Create(
            visit, request.ReffId, request.Descriptions, request.Qty,
            aktifitas, primaryPpa, request.UserId, occurredAt,
            layananId: effectiveLayananId,
            listPpa: listPpa);
        visit.RecordTindakanEvent(tindakan, tindakan.Audit);

        IgdTindakanAddResponse response;
        using (var trans = TransHelper.NewScope())
        {
            if (aktifitas == ActivityTindakanIgd.Barang)
            {
                var pakaiBrgId = await _igdStockService.DeductBhpAsync(
                    effectiveLayananId,
                    request.ReffId,
                    request.Qty,
                    request.UserId,
                    $"BHP-IGD:{tindakan.TindakanIgdId} {request.Descriptions}",
                    cancellationToken);
                tindakan.SetPakaiBrg(pakaiBrgId);
            }
            else
            {
                var bomResult = await _igdStockService.ProcessTindakanBomAsync(
                    tindakan.TindakanIgdId,
                    request.ReffId,
                    request.Qty,
                    effectiveLayananId,
                    visit.HasReg ? visit.Reg.RegId : string.Empty,
                    request.UserId,
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(bomResult.PakaiBrgId))
                    tindakan.SetPakaiBrg(bomResult.PakaiBrgId);
                if (bomResult.ListBarang.Count > 0)
                    tindakan.SetListBarang(bomResult.ListBarang);
            }

            _tindakanRepo.SaveChanges(tindakan);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdTindakanAddResponse(tindakan.TindakanIgdId, visit.IgdVisitId);
        }

        if (visit.HasReg && reg is not null)
        {
            try
            {
                var posted = await _billingOrchestrator.PostTindakanAsync(visit, reg, tindakan, cancellationToken);
                if (!posted)
                {
                    visit.SetPendingBilling(true);
                    _igdVisitRepo.SaveChanges(visit);
                }
            }
            catch (Exception)
            {
                visit.SetPendingBilling(true);
                _igdVisitRepo.SaveChanges(visit);
            }
        }

        return response;
    }

    private string ResolveEffectiveLayananId(string? requestLayananId, RegModel? reg)
    {
        if (!string.IsNullOrWhiteSpace(requestLayananId))
            return requestLayananId.Trim();
        if (reg is not null && !string.IsNullOrWhiteSpace(reg.Layanan.LayananId))
            return reg.Layanan.LayananId.Trim();
        return (_igdVisitOptions.SmassLayananId ?? string.Empty).Trim();
    }

    private (PpaType PrimaryPpa, List<TindakanIgdPpaType> ListPpa) ResolvePpa(
        string? petugasMedisId,
        List<IgdTindakanPpaCmd> validListPpaInput)
    {
        var ppaCache = new Dictionary<string, PpaType>(StringComparer.OrdinalIgnoreCase);

        PpaType LoadPpa(string ppaId)
        {
            var trimmedId = ppaId.Trim();
            if (ppaCache.TryGetValue(trimmedId, out var cached))
                return cached;

            var loaded = _ppaRepo.LoadEntity(PpaType.Key(trimmedId))
                .GetValueOrThrow($"Petugas Medis {trimmedId} not found");
            ppaCache[trimmedId] = loaded;
            return loaded;
        }

        var listPpa = new List<TindakanIgdPpaType>();
        PpaType? firstListPpa = null;
        foreach (var item in validListPpaInput)
        {
            var loaded = LoadPpa(item.PpaId);
            firstListPpa ??= loaded;
            listPpa.Add(new TindakanIgdPpaType(
                item.KomponenId?.Trim() ?? string.Empty,
                loaded.PpaId,
                loaded.PpaName));
        }

        var primaryPpa = !string.IsNullOrWhiteSpace(petugasMedisId)
            ? LoadPpa(petugasMedisId)
            : firstListPpa ?? PpaType.Default;

        return (primaryPpa, listPpa);
    }
}
