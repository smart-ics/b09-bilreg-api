 using Ardalis.GuardClauses;
 using Bilreg.Application.AccountingContext.JurnalFeature;
 using Bilreg.Application.AdmisiContext.AntrianFeature;
 using Bilreg.Application.AdmisiContext.BookingFeature;
 using Bilreg.Application.ChargeContext.TindakanFeature;
 using Bilreg.Application.PaymentContext.TrsBillingFeature;
 using Bilreg.Domain.AccountingContext.JurnalFeature;
 using Bilreg.Domain.AdmisiContext.AntrianFeature;
 using Bilreg.Domain.AdmisiContext.BookingFeature;
 using Bilreg.Domain.AdmisiContext.LayananFeature;
 using Bilreg.Domain.AdmisiContext.PpaFeature;
 using Bilreg.Domain.AdmisiContext.RegFeature;
 using Bilreg.Domain.ChargeContext.TindakanFeature;
 using Bilreg.Domain.PaymentContext.TrsBillingFeature;
 using MediatR;
 using Nuna.Lib.TransactionHelper;
 using Nuna.Lib.ValidationHelper;

 namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanBatalCmd(string RegId, string UserId) : IRequest, IRegKey;

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
    public RegJalanBatalHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IAntrianRepo antrianRepo,
        ITindakanRepo tdkRepo,
        ITrsBillingRepo bilingRepo,
        IAntrianMapRepo antrianMapRepo,
        IPasienTrackerRepo pasienTrackerRepo,
        IJurnalRepo jurnalRepo,
        IDashboardEmrRemoveRegService dashboardEmrRemoveRegService,
        IBookingRepo bookingRepo)
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
    }

    public Task Handle(RegJalanBatalCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reg = LoadReg(request);
        if (reg is null)
            return Task.CompletedTask;
        if (reg.IsAktif == false)
            throw new KeyNotFoundException($"Register {request.RegId} sudah tidak aktif");
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
            VoidReg(reg, request.UserId);
            VoidAntrian(antrianContext);
            VoidAntrianMap(queMap, antrianContext.NoUrut);
            VoidTindakan(tindakanList, request.UserId);
            VoidBilling(billingList);
            _regAktifRepo.Delete(reg);

            trans.Complete();
        }
        var removeReg = new RemoveRegCmd(reg.RegId);
        _dashboardEmrRemoveRegService.Execute(removeReg);

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

    private List<TrsBillingType> LoadAndValidateBilling(RegJalanBatalCmd request)
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
    private void VoidAntrianMap(AntrianMapModel queMap, int noUrut)
    {
        if (queMap.JadwalId == "-")
            return;

        queMap.VoidSlot(noUrut);
        _antrianMapRepo.SaveChanges(queMap);
    }
    private void VoidTindakan(IEnumerable<TindakanModel> listTindakan, string userId)
    {
        foreach (var tindakan in listTindakan)
        {
            tindakan.Void(userId);
            _tdkRepo.SaveChanges(tindakan);
        }
    }
    private void VoidBilling(IEnumerable<TrsBillingType> listBill)
    {
        foreach (var bill in listBill)
        {
            _jurnalRepo.DeleteEntity(JurnalType.Key(bill.TrsBillingId));
            _bilingRepo.DeleteEntity(TrsBillingType.Key(bill.TrsBillingId));
        }

    }


    #endregion
}
