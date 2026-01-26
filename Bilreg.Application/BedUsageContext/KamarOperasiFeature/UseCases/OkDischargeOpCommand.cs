using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkDischargeOpCommand(string OrderOpId,
    string dischargeDate, string dischargeTime,
    int patientCondition, string KamarId,
    string postOpNote, string userId) : IOrderOpKey, IKamarKey, IRequest<OkDischargeOpResponse>;

public record OkDischargeOpResponse(string OrderOpId, string CurrentState, string DischargeAt, KamarReff targetWard);

public class OkDischargeOpHandler : IRequestHandler<OkDischargeOpCommand, OkDischargeOpResponse>
{
    private readonly IDischargeOpRepo _dischargeOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkDischargeOpHandler(IDischargeOpRepo dischargeOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IOpCaseRepo opCaseRepo)
    {
        _dischargeOpRepo = dischargeOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkDischargeOpResponse> Handle(OkDischargeOpCommand request, CancellationToken cancellationToken)
    {
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar ID {request.KamarId} tidak ditemukan.");

        var dischargeDateTime = DateTime.ParseExact($"{request.dischargeDate} {request.dischargeTime}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var newDischarge = DischargeOpModel.Create(orderOp, dischargeDateTime, kamar, (PatientConditionEnum)request.patientCondition,
            request.postOpNote, request.userId);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Discharge(newDischarge.ToReff());

        using var trans = TransHelper.NewScope();
        _dischargeOpRepo.SaveChanges(newDischarge);
        trans.Complete();

        return Task.FromResult(new OkDischargeOpResponse(orderOp.OrderOpId, OpCaseStateEnum.Discharged.ToString(),
            $"{ request.dischargeDate} { request.dischargeTime}", new KamarReff(kamar.KamarId, kamar.KamarName)));
    }
}
