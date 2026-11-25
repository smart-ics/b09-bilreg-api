using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkStartOpCommand(string OrderOpId,
    string Jam, string UserId) : IRequest<OkStartOpResponse>, IOrderOpKey;

public record OkStartOpResponse(
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId);

public class OkStartOpHandler : IRequestHandler<OkStartOpCommand, OkStartOpResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IPreOpClearanceRepo _preOpClearanceRepo;
    private readonly IStartOpRepo _startOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkStartOpHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo,
        IPreOpClearanceRepo preOpClearanceRepo,
        IStartOpRepo startOpRepo,
        IKamarRepo kamarRepo,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
        _preOpClearanceRepo = preOpClearanceRepo;
        _startOpRepo = startOpRepo;
        _kamarRepo = kamarRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkStartOpResponse> Handle(OkStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
                           ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId == null)
            throw new KeyNotFoundException("Schedule Operasi tidak ditemukan.");

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrThrow("Schedule Operasi tidak ditemukan.");

        // Hard Gate 1: Jadwal harus resmi (bukan draf)
        if (scheduleOp.IsDraft)
            throw new InvalidOperationException(
                "Tindakan operasi tidak dapat dimulai karena jadwal operasi masih berstatus DRAFT.");

        // Hard Gate 2: OpCase harus sudah PreOpCleared
        if (opCase.OrderOpState != OpCaseStateEnum.PreOpCleared)
            throw new InvalidOperationException(
                $"Tindakan operasi hanya dapat dimulai setelah Pre-Operative Clearance selesai " +
                $"(status PreOpCleared). Status saat ini: {opCase.OrderOpState}");

        // Hard Gate 3: Clearance aktif harus berstatus CLEARED atau EMERGENCY_OVERRIDDEN
        var clearanceOpt = _preOpClearanceRepo.GetLatestByScheduleOpId(scheduleOp.ScheduleOpId);
        if (!clearanceOpt.HasValue)
            throw new InvalidOperationException(
                "Catatan Pre-Operative Clearance tidak ditemukan untuk jadwal ini.");

        var activeClearance = clearanceOpt.Value;
        if (activeClearance.ClearanceStatus is not (PreOpClearanceStatusEnum.Cleared or PreOpClearanceStatusEnum.EmergencyOverridden))
            throw new InvalidOperationException(
                "Tindakan operasi hanya dapat dimulai jika Pre-Operative Clearance berstatus " +
                "CLEARED atau EMERGENCY_OVERRIDDEN.");

        var tglOp = DateTime.ParseExact(
            $"{ scheduleOp.TglOp.ToString(DateFormatEnum.YMD) } { request.Jam }", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(scheduleOp.KamarOp.KamarId))
            .GetValueOrDefault(KamarType.Default);
        var createdAt = _tglJamProvider.Now;
        var startOp = StartOpModel.CreateFromSchedule(
            scheduleOp, tglOp, kamar, activeClearance.ClearanceId, request.UserId, createdAt);

        opCase.Start(tglOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
        _startOpRepo.SaveChanges(startOp);
        trans.Complete();

        return Task.FromResult(Response(opCase));
    }

    private OkStartOpResponse Response(OpCaseModel model)
    {
        return new OkStartOpResponse(
            OrderOpId: model.OrderOpId,
            ScheduleOpId: model.ScheduleOp.ScheduleOpId,
            RegId: model.Reg.RegId,
            PasienId: model.Pasien.PasienId
        );
    }
}
