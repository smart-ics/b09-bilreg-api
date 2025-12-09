using Ardalis.GuardClauses;
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
        string UserId) : IRequest<OkScheduleOpSetResponse>, IOrderOpKey;

public record OkScheduleOpSetResponse(string ScheduleOpId);

public class OkScheduleOpSetCommandHandler : IRequestHandler<OkScheduleOpSetCommand, OkScheduleOpSetResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkScheduleOpSetCommandHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _opCaseRepo = opCaseRepo;
    }

    public async Task<OkScheduleOpSetResponse> Handle(OkScheduleOpSetCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        var teamLead = PpaType.Default;

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule?
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId)
            ?? new ScheduleOpView("-", new PasienReff("-", "-", DateOnly.ParseExact("3000-01-01", "yyyy-MM-dd"), "-"),
            new OrderOpReff("-", DateTime.ParseExact("3000-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "-"),
            UrgencyLevelEnum.Elective, DateTime.ParseExact("3000-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            0, new PpaReff("-", "-"), new KamarReff("-", "-"));

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrDefault();

        DateTime tglOp = DateTime.ParseExact($"{request.Tgl} {request.Jam}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        if (scheduleOp is null)
        {
            // Kasus 1: ScheduleOp belum ada -> Buat baru dari OrderOp
            scheduleOp = ScheduleOpModel.CreateFromOrder(orderOp, request.UserId,
                kamar, teamLead, tglOp);
        }
        else
        {
            // Kasus 2: ScheduleOp sudah ada -> Panggil behavior untuk Set Schedule
            // Memastikan logic validasi dan mutasi berada dalam Domain Model (ScheduleOpModel)
            scheduleOp.SetSchedule(tglOp, kamar.ToReff(), request.UserId);
        }

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Schedule(scheduleOp.ToReff());

        using var trans = TransHelper.NewScope();
        _scheduleOpRepo.SaveChanges(scheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return new OkScheduleOpSetResponse(scheduleOp.ScheduleOpId);
    }
}
