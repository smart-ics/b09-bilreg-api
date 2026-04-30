using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegOutCmd(string RegId, string TglJamKeluar, string UserId) : IRequest, IRegKey;

public class RegOutHandler : IRequestHandler<RegOutCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegPembayaranRepo _regPembayaranRepo;
    private readonly ITrsBillingBayarRepo _trsBillingBayarRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ICoaRepo _coaRepo;
    public RegOutHandler(IRegRepo regRepo, 
        IRegPembayaranRepo regPembayaranRepo,
        ITrsBillingBayarRepo trsBillingBayarRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IJaminanRepo jaminanRepo,
        ICoaRepo coaRepo)
    {
        _regRepo = regRepo;
        _regPembayaranRepo = regPembayaranRepo;
        _trsBillingBayarRepo = trsBillingBayarRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _jaminanRepo = jaminanRepo;
        _coaRepo = coaRepo;
    }
    public Task Handle(RegOutCmd request, CancellationToken cancellationToken)
    {
        var reg = LoadReg(request);
        if (!reg.IsAktif)
            throw new KeyNotFoundException($"Register {request.RegId} sudah tidak aktif");

        var listPembayaranRegOut = _regPembayaranRepo.ListData(request).ToList();
        if (!listPembayaranRegOut.Any())
            throw new KeyNotFoundException($"Data pembayaran untuk Register {request.RegId} tidak ditemukan.{Environment.NewLine}Registrasi keluar dibatalkan");

        // Merge BYVCH ke BYDPU
        var processedPembayaran = MergeVoucherToDpu(listPembayaranRegOut).ToList();

        // Pre-resolve mapping CaraBayarId => JenisBayarId (KAS/HUT/...)
        var jenisBayarMap = PreResolveJenisBayarMapping(processedPembayaran);

        // Inject mapping ke Domain Factory
        var listBilling2 = _trsBillingBayarRepo.ListData(request).ToList();
        var billing2RegOut = TrsBilling2Model.CreateFromRegKeluar(processedPembayaran, listBilling2, jenisBayarMap, request.TglJamKeluar).ToList();

        reg.Keluar(request.TglJamKeluar, request.UserId);

        // Finalize Aggregate
        _trsBillingBayarRepo.SaveChanges(billing2RegOut);
        _regRepo.SaveChanges(reg);

        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    private RegModel LoadReg(RegOutCmd request)
    {
        var reg = _regRepo.LoadEntity(request);
        return reg.HasValue ? reg.Value : throw new KeyNotFoundException($"Register {request.RegId} tidak ditemukan");
    }

    private IEnumerable<RegPembayaranType> MergeVoucherToDpu(IEnumerable<RegPembayaranType> source)
    {
        return source
            .GroupBy(p => new { p.RegId, NormalizedId = p.CaraBayarId == "BYVCH" ? "BYDPU" : p.CaraBayarId })
            .Select(g =>
            {
                var first = g.First();
                var finalName = g.Any(x => x.CaraBayarId == "BYDPU")
                    ? g.First(x => x.CaraBayarId == "BYDPU").CaraBayarName
                    : first.CaraBayarName;

                return new RegPembayaranType(
                    first.RegId, g.Key.NormalizedId, finalName,
                    g.Sum(x => x.NilaiJasa), g.Sum(x => x.NilaiObat), g.Sum(x => x.NilaiSubTotal));
            });
    }

    private IReadOnlyDictionary<string, string> PreResolveJenisBayarMapping(IEnumerable<RegPembayaranType> pembayaranTypes)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in pembayaranTypes)
        {
            if (!map.ContainsKey(p.CaraBayarId))
                map[p.CaraBayarId] = ResolveJenisBayar(p.CaraBayarId);
        }
        return map;
    }

    private string ResolveJenisBayar(string caraBayarId) => caraBayarId switch
    {
        "BYDPK" or "BYDPU" or "BYKAS" => "KAS",
        "BYPRI" => "HUT",
        _ => ResolveFromJaminanRule(caraBayarId)
    };

    private string ResolveFromJaminanRule(string caraBayarId)
    {
        var tipeJaminanKey = TipeJaminanType.Key(caraBayarId);
        var tipejaminan = _tipeJaminanRepo.LoadEntity(tipeJaminanKey);
        if(!tipejaminan.HasValue)
            return caraBayarId;

        var jaminan = _jaminanRepo.LoadEntity(tipejaminan.Value.Jaminan);
        if (!jaminan.HasValue || jaminan.Value.Rekening.PiutangPulang.CoaId == "-") 
            return caraBayarId;

        var coa = _coaRepo.LoadEntity(jaminan.Value.Rekening.PiutangPulang);
        if (!coa.HasValue) 
            return caraBayarId;

        var kasTipeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "KAS", "KAS BANK", "GR MASUK", "JUAL DISC" };

        return kasTipeSet.Contains(coa.Value.CoaTipeType.CoaTipeId) ? "KAS" : caraBayarId;
    }

    #endregion
}
