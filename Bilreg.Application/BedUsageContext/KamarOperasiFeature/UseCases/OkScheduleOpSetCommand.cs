using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpSetCommand(
        string OrderOpId,
        string KamarId,
        string Tgl,
        string Jam,
        int Durasi,
        string UserId) : IRequest<OkScheduleOpSetResponse>, IOrderOpKey;

public record OkScheduleOpSetResponse(string ScheduleOpId);

public class OkScheduleOpSetCommandHandler : IRequestHandler<OkScheduleOpSetCommand, OkScheduleOpSetResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkScheduleOpSetCommandHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkScheduleOpSetResponse> Handle(OkScheduleOpSetCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);

        var scheduleOp = scheduleWithOrderOpId != null
            ? _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
                .GetValueOrDefault()
            : null;

        var teamLeadExisting = scheduleOp?.TeamLead;
        var teamLead = teamLeadExisting != null ? _ppaRepo.LoadEntity(PpaType.Key(teamLeadExisting.PpaId))
            .GetValueOrDefault()
            ?? PpaType.Default : PpaType.Default;

        DateTime tglOp = DateTime.ParseExact($"{request.Tgl} {request.Jam}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        ScheduleOpModel newScheduleOp;
        if (scheduleOp != null)
        {
            scheduleOp.CancelSchedule(request.UserId);
            newScheduleOp = ScheduleOpModel.CloneFrom(scheduleOp);
        }
        else
            newScheduleOp = ScheduleOpModel.CreateFromOrder(orderOp, request.UserId,
                 kamar, teamLead, tglOp);
        newScheduleOp.SetSchedule(tglOp, kamar.ToReff(), request.Durasi, request.UserId);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Schedule(newScheduleOp.ToReff());

        using var trans = TransHelper.NewScope();
        if (scheduleOp != null)
            _scheduleOpRepo.SaveChanges(scheduleOp);
        _scheduleOpRepo.SaveChanges(newScheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(new OkScheduleOpSetResponse(newScheduleOp.ScheduleOpId));
    }
}
