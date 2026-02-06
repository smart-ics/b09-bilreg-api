using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCancelStartOpCommand(string OrderOpId,
    string UserId) : IRequest<OkCancelStartOpResponse>, IOrderOpKey;

public record OkCancelStartOpResponse(
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId);

public class OkCancelStartOpHandler : IRequestHandler<OkCancelStartOpCommand, OkCancelStartOpResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkCancelStartOpHandler(IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkCancelStartOpResponse> Handle(OkCancelStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

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
