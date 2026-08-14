using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

 namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanBatalCmd(string RegId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, IRegKey;

public class RegJalanBatalHandler : IRequestHandler<RegJalanBatalCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITindakanRepo _tdkRepo;
    private readonly ITrsBillingRepo _bilingRepo;
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IPasienTrackerRepo _pasienTrackerRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IDashboardEmrRemoveRegService _dashboardEmrRemoveRegService;
    private readonly IBookingRepo _bookingRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IQueueNumberCompatibilityAdapter _queueNumberAdapter;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IEmrAntrianOutboundQueueRepo _emrAntrianOutboundQueueRepo;
    private readonly IIgdVisitRepo _igdVisitRepo;
    public RegJalanBatalHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IAntrianRepo antrianRepo,
        ITindakanRepo tdkRepo,
        ITrsBillingRepo bilingRepo,
        IAntrianMapRepo antrianMapRepo,
        IPasienTrackerRepo pasienTrackerRepo,
        IJurnalRepo jurnalRepo,
        IDashboardEmrRemoveRegService dashboardEmrRemoveRegService,
        IBookingRepo bookingRepo,
        IAuditRepo auditRepo,
        IQueueNumberCompatibilityAdapter queueNumberAdapter,
        ITglJamProvider tglJamProvider,
        IEmrAntrianOutboundQueueRepo emrAntrianOutboundQueueRepo,
        IIgdVisitRepo igdVisitRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _antrianRepo = antrianRepo;
        _tdkRepo = tdkRepo;
        _bilingRepo = bilingRepo;
        _antrianMapRepo = antrianMapRepo;
        _pasienTrackerRepo = pasienTrackerRepo;
        _jurnalRepo = jurnalRepo;
        _dashboardEmrRemoveRegService = dashboardEmrRemoveRegService;
        _bookingRepo = bookingRepo;
        _auditRepo = auditRepo;
        _queueNumberAdapter = queueNumberAdapter;
        _tglJamProvider = tglJamProvider;
        _emrAntrianOutboundQueueRepo = emrAntrianOutboundQueueRepo;
        _igdVisitRepo = igdVisitRepo;
    }

    public Task Handle(RegJalanBatalCmd request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reg = LoadReg(request);
        if (reg is null)
            return Task.CompletedTask;
        if (reg.IsAktif == false)
            throw new KeyNotFoundException($"Register {request.RegId} sudah tidak aktif");
        var snapshotJson = AuditLogSnapshotJson.Serialize(reg);

        var igdVisit = _igdVisitRepo.GetByRegId(request.RegId);
        if (igdVisit.HasValue)
        {
            throw new InvalidOperationException(
                $"Registrasi {request.RegId} terhubung dengan IGD Visit '{igdVisit.Value.IgdVisitId}'. Batalkan IGD Visit terlebih dahulu.");
        }

        var book = BookingModel.Default;
        var periode = new Periode(reg.RegDate.ToDateTime(TimeOnly.MinValue));
        var listBook = _bookingRepo.ListDataTglBerobat(periode)?.ToList() ?? [];
        var bookDto = listBook.FirstOrDefault(x => x.Reg.RegId == reg.RegId);
        if (bookDto is not null)
        {
            var bookKey = BookingModel.Key(bookDto.BookingId);
            book = _bookingRepo.LoadEntity(bookKey).GetValueOrDefault(BookingModel.Default);
            book.UnRegister();
        }

        var tindakanList = LoadAndValidateTindakan(request);
        var antrianContext = LoadAntrianContext(reg);
        var queMap = LoadAntrianMap(reg, antrianContext.Que);
        var billingList = LoadAndValidateBilling(request)?.ToList() ?? [];
        
        using (var trans = TransHelper.NewScope())
        {
            if (book.BookingId != "-")
                _bookingRepo.SaveChanges(book);
            VoidReg(reg, request.UserId, occurredAt);
            VoidAntrian(antrianContext, reg.RegId, occurredAt);
            VoidAntrianMap(queMap, antrianContext.NoUrut);
            VoidTindakan(tindakanList, request.UserId, occurredAt);
            VoidBilling(billingList);
            _regAktifRepo.Delete(reg);
            
            _emrAntrianOutboundQueueRepo.DeleteBySource(reg.RegId);

            trans.Complete();
        }
        var removeReg = new RemoveRegCmd(reg.RegId);
        _dashboardEmrRemoveRegService.Execute(removeReg);

        var audit = CreateAudit(reg, snapshotJson, request, occurredAt);
        _auditRepo.SaveChanges(audit);

        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    // LOAD-DATA
    private RegModel? LoadReg(RegJalanBatalCmd request)
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
    private List<TindakanModel> LoadAndValidateTindakan(RegJalanBatalCmd request)
    {
        var list = _tdkRepo.ListData(request)?.ToList() ?? [];
        if (list.Count > 1)
            throw new InvalidOperationException($"void Tindakan atas Register {request.RegId} terleboh dahulu");

        return list
            .Select(x => _tdkRepo.LoadEntity(x).Value)
            .ToList();
    }

    private List<TrsBillView> LoadAndValidateBilling(RegJalanBatalCmd request)
    {
        var billingList = _bilingRepo.ListData(request)?.ToList() ?? [];

        if (billingList.Count() > 2)
            throw new ArgumentException("Pasien ini masih memiliki Bill, void bill terlebih dahulu");
        return billingList;
    }
    private AntrianMapModel LoadAntrianMap(RegModel reg, AntrianModel que)
    {
        var ppaKey = PpaType.Key(reg.Dokter.PpaId);
        var lynKey = LayananType.Key(reg.Layanan.LayananId);
        
        var listAntrianMap = _antrianMapRepo
            .ListData(lynKey, ppaKey, reg.RegDate)?
            .ToList() ?? [];
        var antrianThis = listAntrianMap.FirstOrDefault(x => x.JamJadwal == que.StartTime);

        var result = antrianThis is null
            ? AntrianMapModel.Default
            : _antrianMapRepo
                .LoadEntity(AntrianMapModel.Key(antrianThis.AntrianMapId))
                .Value;

        return result;
    }
    private static AuditLog CreateAudit(RegModel reg, string snapShotJson, RegJalanBatalCmd cmd, DateTime occurredAt)
    {
        var result = AuditLog.Create(
            reg.RegVoidAudit.UserId,
            occurredAt,
            actionType: "VOID",
            entityName: nameof(RegModel),
            entityId: reg.RegId,
            reason: cmd.VoidReason,
            originalDataJson: snapShotJson,
            correlationId: reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
    // VOID
    private void VoidReg(RegModel reg, string userId, DateTime occurredAt)
    {
        reg.BatalBerobat(userId, occurredAt);
        _regRepo.SaveChanges(reg);
    }
    private void VoidAntrian(
        (AntrianModel Que, int NoUrut, IPasienTrackerKey TrackerKey) ctx,
        string regId,
        DateTime occurredAt)
    {
        if (ctx.Que.AntrianId != "-")
        {
            ctx.Que.RemoveEntry(ctx.NoUrut);
            _antrianRepo.SaveChanges(ctx.Que);
        }

        // Retain Tracker evidence; append cancellation (BR-TRK-009a/b/c).
        if (!PasienTrackerStableIdentity.IsRealTrackerId(ctx.TrackerKey.PasienTrackerId))
            return;

        var trackerOpt = _pasienTrackerRepo.LoadEntity(ctx.TrackerKey);
        if (!trackerOpt.HasValue)
            return;

        var tracker = trackerOpt.Value;
        tracker.AddEvent("REGISTER_CANCELLED", regId, occurredAt);
        _pasienTrackerRepo.SaveChanges(tracker);
    }
    private void VoidAntrianMap(AntrianMapModel queMap, int noUrut)
    {
        if (queMap.JadwalId == "-")
            return;

        _queueNumberAdapter.Release(queMap, noUrut);
        _antrianMapRepo.SaveChanges(queMap);
    }
    private void VoidTindakan(IEnumerable<TindakanModel> listTindakan, string userId, DateTime occurredAt)
    {
        foreach (var tindakan in listTindakan)
        {
            tindakan.Void(userId, occurredAt);
            _tdkRepo.SaveChanges(tindakan);
        }
    }
    private void VoidBilling(IEnumerable<TrsBillView> listBill)
    {
        foreach (var bill in listBill)
        {
            _jurnalRepo.DeleteEntity(JurnalType.Key(bill.TrsBillingId));
            _bilingRepo.DeleteEntity(TrsBillType.Key(bill.TrsBillingId));
        }

    }


    #endregion
}
