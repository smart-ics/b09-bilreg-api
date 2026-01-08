using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCancelStartOpCommand(string ScheduleOpId,
    string UserId) : IRequest<OkCancelStartOpResponse>, IScheduleOpKey;

public record OkCancelStartOpResponse(
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId);

public class OkCancelStartOpHandler : IRequestHandler<OkCancelStartOpCommand, OkCancelStartOpResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkCancelStartOpHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkCancelStartOpResponse> Handle(OkCancelStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi ID {request.ScheduleOpId} tidak ditemukan.");

        if (scheduleOp.OrderOp.OrderOpId == "-")
            throw new KeyNotFoundException($"Schedule Operasi ID {request.ScheduleOpId} tidak punya order.");

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(scheduleOp.OrderOp.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi ID {request.ScheduleOpId} tidak punya order.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.CancelStart();

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(Response(opCase));
    }

    private OkCancelStartOpResponse Response(OpCaseModel model)
    {
        return new OkCancelStartOpResponse(
            OrderOpId: model.OrderOpId,
            ScheduleOpId: model.ScheduleOp.ScheduleOpId,
            RegId: model.Reg.RegId,
            PasienId: model.Pasien.PasienId
        );
    }
}
