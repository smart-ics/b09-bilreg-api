using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
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

    public OkStartOpHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkStartOpResponse> Handle(OkStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.");
        
        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
                           ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);

        var scheduleOp = scheduleWithOrderOpId != null
            ? _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
                .GetValueOrThrow($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.")
            : throw new KeyNotFoundException($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.");

        var tglOp = DateTime.ParseExact(
            $"{ scheduleOp.TglOp.ToString(DateFormatEnum.YMD) } { request.Jam }", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Start(tglOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
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
