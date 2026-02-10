using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegBatalCmd(string RegId, string UserId) : IRequest, IRegKey;

public class RegBatalHandler : IRequestHandler<RegBatalCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITindakanRepo _tdkRepo;
    private readonly ITrsBillingRepo _bilingRepo;
    private readonly IAntrianMapHdrRepo _antrianMapHdrRepo;
    private readonly IPasienTrackerRepo _pasienTrackerRepo;

    public RegBatalHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IAntrianRepo antrianRepo,
        ITindakanRepo tdkRepo,
        ITrsBillingRepo bilingRepo,
        IAntrianMapHdrRepo antrianMapHdrRepo,
        IPasienTrackerRepo pasienTrackerRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _antrianRepo = antrianRepo;
        _tdkRepo = tdkRepo;
        _bilingRepo = bilingRepo;
        _antrianMapHdrRepo = antrianMapHdrRepo;
        _pasienTrackerRepo = pasienTrackerRepo;
    }

    public Task Handle(RegBatalCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reg = LoadReg(request);
        if (reg is null)
            return Task.CompletedTask;

        var tindakanList = LoadAndValidateTindakan(request);
        var antrianContext = LoadAntrianContext(reg);
        var queMap = LoadAntrianMap(reg, antrianContext.Que);
        var billingList = _bilingRepo.ListData(request)?.ToList() ?? [];

        using var trans = TransHelper.NewScope();

        VoidReg(reg, request.UserId);
        VoidAntrian(antrianContext);
        VoidAntrianMap(queMap, antrianContext.NoUrut);
        VoidTindakan(tindakanList, request.UserId);
        VoidBilling(billingList);
        _regAktifRepo.Delete(reg);

        trans.Complete();
        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    // LOAD-DATA
    private RegModel? LoadReg(RegBatalCmd request)
    {
        return _regRepo.LoadEntity(request).GetValueOrDefault(RegModel.Default);
    }
    
    private (AntrianModel Que, int NoUrut, IPasienTrackerKey TrackerKey) LoadAntrianContext(RegModel reg)
    {
        var queDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var listQue = _antrianRepo.ListData(queDate)?.ToList() ?? [];

        var queReg = listQue.FirstOrDefault(x => x.ReffId == reg.RegId);
        if (queReg is null)
            return (AntrianModel.Default, 0, PasienTrackerModel.Key("-"));

        var que = _antrianRepo
            .LoadEntity(AntrianModel.Key(queReg.AntrianId))
            .GetValueOrDefault(AntrianModel.Default);

        var entry = que.ListEntry
            .FirstOrDefault(x => x.NoUrut == queReg.NoUrut)
            ?? AntrianEntryModel.Default;

        return (
            que,
            queReg.NoUrut,
            PasienTrackerModel.Key(entry.Tracker.PasienTrackerId)
        );
    }

    private List<TindakanModel> LoadAndValidateTindakan(RegBatalCmd request)
    {
        var list = _tdkRepo.ListData(request)?.ToList() ?? [];
        if (list.Count > 1)
            throw new InvalidOperationException($"void Tindakan atas Register {request.RegId} terleboh dahulu");

        return list
            .Select(x => _tdkRepo.LoadEntity(x).Value)
            .ToList();
    }

    private AntrianMapHdrModel LoadAntrianMap(RegModel reg, AntrianModel que)
    {
        var ppaKey = PpaType.Key(reg.Dokter.PpaId);
        var lynKey = LayananType.Key(reg.Layanan.LayananId);
        var listAntrianMap = _antrianMapHdrRepo
            .ListData(lynKey, ppaKey, reg.RegDate)?
            .ToList() ?? [];

        var antrianThis = listAntrianMap.SingleOrDefault(x => x.JamJadwal == que.StartTime);

        if (antrianThis is not null)
        {
            var antKey = AntrianMapHdrModel.Key(
                antrianThis.JadwalId,
                antrianThis.TglJadwal,
                antrianThis.dokter.PpaId,
                antrianThis.Layanan.LayananId,
                antrianThis.JamJadwal);

            return _antrianMapHdrRepo.LoadEntity(antKey).Value;
        }
        else
            return AntrianMapHdrModel.Default;
    }

    // VOID
    private void VoidReg(RegModel reg, string userId)
    {
        reg.BatalBerobat(userId);
        _regRepo.SaveChanges(reg);
    }

    private void VoidAntrian((AntrianModel Que, int NoUrut, IPasienTrackerKey TrackerKey) ctx)
    {
        if (ctx.Que.AntrianId == "-")
            return;

        ctx.Que.RemoveEntry(ctx.NoUrut);
        _antrianRepo.SaveChanges(ctx.Que);

        _pasienTrackerRepo.DeleteEntity(ctx.TrackerKey);
    }

    private void VoidAntrianMap(AntrianMapHdrModel queMap, int noUrut)
    {
        if (queMap.JadwalId == "-")
            return;

        queMap.VoidSlot(noUrut);
        _antrianMapHdrRepo.SaveChanges(queMap);
    }

    private void VoidTindakan(IEnumerable<TindakanModel> listTindakan, string userId)
    {
        foreach (var tindakan in listTindakan)
        {
            tindakan.Void(userId);
            _tdkRepo.SaveChanges(tindakan);
        }
    }
    private void VoidBilling(IEnumerable<TrsBillingView> listBill)
    {
        foreach (var bill in listBill)
            _bilingRepo.DeleteEntity(
                TrsBillingType.Key(bill.TrsBillingId));
    }

    #endregion
}
