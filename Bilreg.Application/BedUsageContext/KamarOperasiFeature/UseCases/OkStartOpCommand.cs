using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkStartOpCommand(string ScheduleOpId,
    string Jam, string UserId) : IRequest<OkStartOpResponse>, IScheduleOpKey;

public record OkStartOpResponse(
    string StartOpId,
    string OrderOpId,
    string ScheduleOpId,
    string NamaOperasi,
    string RegId,
    string PasienId,
    string PasienName);

public class OkStartOpHandler : IRequestHandler<OkStartOpCommand, OkStartOpResponse>
{
    private readonly IStartOpRepo _startOpRepo;
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkStartOpHandler(IStartOpRepo startOpRepo,
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IOpCaseRepo opCaseRepo)
    {
        _startOpRepo = startOpRepo;
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkStartOpResponse> Handle(OkStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi ID { request.ScheduleOpId } tidak ditemukan.");

        if (scheduleOp.OrderOp.OrderOpId == "-")
            throw new KeyNotFoundException($"Schedule Operasi ID { request.ScheduleOpId } tidak punya order.");

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(scheduleOp.OrderOp.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi ID { request.ScheduleOpId } tidak punya order.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(scheduleOp.KamarOp.KamarId))
            .GetValueOrDefault() ?? KamarType.Default;

        var tglOp = DateTime.ParseExact(
            $"{ scheduleOp.TglOp.ToString(DateFormatEnum.YMD) } { request.Jam }", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var startOp = StartOpModel.CreateFromSchedule(scheduleOp, tglOp, kamar, request.UserId);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Start();

        //  WRITE
        using var trans = TransHelper.NewScope();
        _startOpRepo.SaveChanges(startOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(Response(startOp));
    }

    private OkStartOpResponse Response(StartOpModel startOp)
    {
        return new OkStartOpResponse(
            StartOpId: startOp.StartOpId,
            OrderOpId: startOp.OrderOp.OrderOpId,
            ScheduleOpId: startOp.ScheduleOp.ScheduleOpId,
            NamaOperasi: startOp.OrderOp.NamaOperasi,
            RegId: startOp.Reg.RegId,
            PasienId: startOp.Pasien.PasienId,
            PasienName: startOp.Pasien.PasienName
        );
    }
}
